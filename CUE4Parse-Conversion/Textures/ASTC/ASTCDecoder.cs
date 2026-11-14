using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.Textures.ASTC;

public static partial class ASTCDecoder
{
    public const int AstcBlockSize = 16;

    public enum AstcChannelMode
    {
        RGBA,
        RG0A, // Normal textures, restore blue from RG
        RRRG, // ASTC high precision NormalRG support, restore blue from RG and alpha 0xFF
    }

    public static byte[] DecodeASTC(byte[] input, int sizeX, int sizeY, int sizeZ, int blockSizeX, int blockSizeY, AstcChannelMode channelMode = AstcChannelMode.RGBA)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, blockSizeX, blockSizeY);

        var inputSpan = input.AsSpan();
        var outputSpan = output.AsSpan();
        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            DecodeASTC(inputSpan.Slice(inputOffset, inputLayerSize), sizeX, sizeY, outputSpan.Slice(outputOffset, outputLayerSize), blockSizeX, blockSizeY, channelMode);
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void DecodeASTC(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output, int blockSizeX, int blockSizeY, AstcChannelMode channelMode = AstcChannelMode.RGBA)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, blockSizeX, blockSizeY);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            DecodeASTC(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize), blockSizeX, blockSizeY, channelMode);
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void DecodeASTC(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output, int blockSizeX, int blockSizeY, AstcChannelMode channelMode = AstcChannelMode.RGBA)
    {
        var (_, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, blockSizeX, blockSizeY);

        Span<uint> result = stackalloc uint[blockSizeX * blockSizeY];
        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();
        BlockData blockData = new() { BlockWidth = blockSizeX, BlockHeight = blockSizeY};

        var offset = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += blockSizeY)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += blockSizeX, offset += AstcBlockSize)
            {
                DecodeAstcBlock(input.Slice(offset, AstcBlockSize), result, ref blockData);

                int width = Math.Min(blockSizeX, sizeX - x);
                int height = Math.Min(blockSizeY, sizeY - y);

                var dstOffset = xPixelOffset;
                int srcOffset = 0;
                for (int row = 0; row < height; row++)
                {
                    result.Slice(srcOffset, width).CopyTo(outputSpan.Slice(dstOffset, width));

                    srcOffset += blockSizeX;
                    dstOffset += sizeX;
                }

                xPixelOffset += blockSizeX;
            }
            yPixelOffset += blockSizeY * sizeX;
        }

        if (channelMode == AstcChannelMode.RG0A)
        {
            for (offset = 0; offset < outputLayerSize; offset += 4)
            {
                output[offset + 2] = BCDecoder.GetZNormal(output[offset], output[offset + 1]);
            }
        }
        else if (channelMode == AstcChannelMode.RRRG)
        {
            for (offset = 0; offset < outputLayerSize; offset += 4)
            {
                output[offset + 2] = BCDecoder.GetZNormal(output[offset], output[offset + 3]);
                output[offset + 3] = 0xFF;
            }
        }
    }

    private static void DecodeAstcBlock(ReadOnlySpan<byte> input, Span<uint> output, ref BlockData blockData)
    {
        if (input[0] == 0xfc && (input[1] & 1) == 1)
        {
            uint c = (uint)(input[15] << 24 | input[13] << 16 | input[11] << 8 | input[9]);
            output.Fill(c);
        }
        else
        {
            var uint128 = BinaryPrimitives.ReadUInt128LittleEndian(input);
            DecodeBlockParameters(uint128, ref blockData);
            DecodeEndpoints(uint128, ref blockData);
            DecodeWeights(input, ref blockData);
            if (blockData.PartNum > 1)
            {
                SelectPartition((uint)uint128, ref blockData);
            }
            ApplicateColor(blockData, output);
        }
    }

    private static void DecodeBlockParameters(UInt128 input, ref BlockData block)
    {
        var inp = (int)input;
        block.DualPlane = (inp >> 10) & 1;
        block.WeightRange = (inp >> 4 & 1) | (inp >> 6 & 8);

        if ((inp & 3) != 0)
        {
            block.WeightRange |= inp << 1 & 6;
            switch (inp & 0xc)
            {
                case 0:
                    block.Width = (inp >> 7 & 3) + 4;
                    block.Height = (inp >> 5 & 3) + 2;
                    break;
                case 4:
                    block.Width = (inp >> 7 & 3) + 8;
                    block.Height = (inp >> 5 & 3) + 2;
                    break;
                case 8:
                    block.Width = (inp >> 5 & 3) + 2;
                    block.Height = (inp >> 7 & 3) + 8;
                    break;
                case 12:
                    if ((inp & 0x100) != 0)
                    {
                        block.Width = (inp >> 7 & 1) + 2;
                        block.Height = (inp >> 5 & 3) + 2;
                    }
                    else
                    {
                        block.Width = (inp >> 5 & 3) + 2;
                        block.Height = (inp >> 7 & 1) + 6;
                    }
                    break;
            }
        }
        else
        {
            block.WeightRange |= inp >> 1 & 6;
            switch (inp & 0x180)
            {
                case 0:
                    block.Width = 12;
                    block.Height = (inp >> 5 & 3) + 2;
                    break;
                case 0x80:
                    block.Width = (inp >> 5 & 3) + 2;
                    block.Height = 12;
                    break;
                case 0x100:
                    block.Width = (inp >> 5 & 3) + 6;
                    block.Height = (inp >> 9 & 3) + 6;
                    block.DualPlane = 0;
                    block.WeightRange &= 7;
                    break;
                case 0x180:
                    block.Width = (inp & 0x20) != 0 ? 10 : 6;
                    block.Height = (inp & 0x20) != 0 ? 6 : 10;
                    break;
            }
        }

        block.PartNum = (inp >> 11 & 3) + 1;

        block.WeightNum = block.Width * block.Height;
        if (block.DualPlane != 0)
        {
            block.WeightNum *= 2;
        }

        int configBits, cemBase = 0;
        var weightRange = block.WeightRange & 0xF;
        int weightBits = block.WeightNum * WeightPrecTableB[weightRange];
        weightBits += WeightPrecTableA[weightRange] switch
        {
            3 => (block.WeightNum * 8 + 4) / 5,
            5 => (block.WeightNum * 7 + 2) / 3,
            _ => 0,
        };
        block.WeightBits = weightBits;
        if (block.PartNum == 1)
        {
            block.Cem[0] = inp >> 13 & 0xf;
            configBits = 17;
        }
        else
        {
            cemBase = inp >> 23 & 3;
            if (cemBase == 0)
            {
                int cem = inp >> 25 & 0xf;
                for (int i = 0; i < block.PartNum; i++)
                {
                    block.Cem[i] = cem;
                }
                configBits = 29;
            }
            else
            {
                for (int i = 0; i < block.PartNum; i++)
                {
                    block.Cem[i] = ((inp >> (i + 25) & 1) + cemBase - 1) << 2;
                }

                var cemData = (byte) (input >> (120 - weightBits));
                switch (block.PartNum)
                {
                    case 2:
                        block.Cem[0] |= inp >> 27 & 3;
                        block.Cem[1] |= (cemData >> 6 & 3);
                        break;
                    case 3:
                        block.Cem[0] |= inp >> 28 & 1;
                        block.Cem[0] |= (cemData >> 2 & 2);
                        block.Cem[1] |= (cemData >> 4 & 3);
                        block.Cem[2] |= (cemData >> 6 & 3);
                        break;
                    case 4:
                        block.Cem[0] |= (cemData >> 0 & 3);
                        block.Cem[1] |= (cemData >> 2 & 3);
                        block.Cem[2] |= (cemData >> 4 & 3);
                        block.Cem[3] |= (cemData >> 6 & 3);
                        break;
                }
                configBits = 25 + block.PartNum * 3;
            }
        }

        if (block.DualPlane != 0)
        {
            configBits += 2;
            block.PlaneSelector = (int)(input >> (cemBase != 0 ? 130 - weightBits - block.PartNum * 3 : 126 - weightBits)) & 3;
        }

        int remainBits = 128 - configBits - weightBits;

        block.EndpointValueNum = 0;
        for (int i = 0; i < block.PartNum; i++)
        {
            block.EndpointValueNum += (block.Cem[i] >> 1 & 6) + 2;
        }

        for (int i = 0; i < CemTableA.Length; i++)
        {
            int endpointBits = block.EndpointValueNum * CemTableB[i];
            endpointBits += CemTableA[i] switch
            {
                3 => (block.EndpointValueNum * 8 + 4) / 5,
                5 => (block.EndpointValueNum * 7 + 2) / 3,
                _ => 0,
            };
            if (endpointBits <= remainBits)
            {
                block.CemRange = i;
                break;
            }
        }
    }

    private static void DecodeEndpoints(UInt128 input, ref BlockData block)
    {
        Span<IntSeqData> epSeq = stackalloc IntSeqData[32];
        var cemA = CemTableA[block.CemRange];
        var cemB = CemTableB[block.CemRange];
        DecodeIntseq(input, block.PartNum == 1 ? 17 : 29, cemA, cemB, block.EndpointValueNum, epSeq);

        Span<int> ev = stackalloc int[32];
        switch (cemA)
        {
            case 3:
                for (int i = 0, b = 0, c = DETritsTable[cemB]; i < block.EndpointValueNum; i++)
                {
                    int a = (epSeq[i].Bits & 1) * 0x1ff;
                    int x = epSeq[i].Bits >> 1;
                    b = cemB switch
                    {
                        1 => 0,
                        2 => 0b100010110 * x,
                        3 => x << 7 | x << 2 | x,
                        4 => x << 6 | x,
                        5 => x << 5 | x >> 2,
                        6 => x << 4 | x >> 4,
                        _ => b
                    };
                    ev[i] = (a & 0x80) | ((epSeq[i].Nonbits * c + b) ^ a) >> 2;
                }
                break;

            case 5:
                for (int i = 0, b = 0, c = DEQuintsTable[cemB]; i < block.EndpointValueNum; i++)
                {
                    int a = (epSeq[i].Bits & 1) * 0x1ff;
                    int x = epSeq[i].Bits >> 1;
                    b = cemB switch
                    {
                        1 => 0,
                        2 => 0b100001100 * x,
                        3 => x << 7 | x << 1 | x >> 1,
                        4 => x << 6 | x >> 1,
                        5 => x << 5 | x >> 3,
                        _ => b
                    };
                    ev[i] = (a & 0x80) | ((epSeq[i].Nonbits * c + b) ^ a) >> 2;
                }
                break;

            default:
                switch (cemB)
                {
                    case 1:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits * 0xff;
                        }
                        break;
                    case 2:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits * 0x55;
                        }
                        break;
                    case 3:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits << 5 | epSeq[i].Bits << 2 | epSeq[i].Bits >> 1;
                        }
                        break;
                    case 4:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits << 4 | epSeq[i].Bits;
                        }
                        break;
                    case 5:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits << 3 | epSeq[i].Bits >> 2;
                        }
                        break;
                    case 6:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits << 2 | epSeq[i].Bits >> 4;
                        }
                        break;
                    case 7:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits << 1 | epSeq[i].Bits >> 6;
                        }
                        break;
                    case 8:
                        for (int i = 0; i < block.EndpointValueNum; i++)
                        {
                            ev[i] = epSeq[i].Bits;
                        }
                        break;
                }
                break;
        }

        Span<int> v = ev;
        for (int cem = 0; cem < block.PartNum; v = v[((block.Cem[cem] / 4 + 1) * 2)..], cem++)
        {
            var endpoint = block.Endpoints[cem][..8];
            switch (block.Cem[cem])
            {
                case 0:
                    SetEndpoint(endpoint, v[0], v[0], v[0], 255, v[1], v[1], v[1], 255);
                    break;
                case 1:
                    int l0 = (v[0] >> 2) | (v[1] & 0xc0);
                    int l1 = Clamp(l0 + (v[1] & 0x3f));
                    SetEndpoint(endpoint, l0, l0, l0, 255, l1, l1, l1, 255);
                    break;
                case 4:
                    SetEndpoint(endpoint, v[0], v[0], v[0], v[2], v[1], v[1], v[1], v[3]);
                    break;
                case 5:
                    BitTransferSigned(ref v[1], ref v[0]);
                    BitTransferSigned(ref v[3], ref v[2]);
                    v[1] += v[0];
                    SetEndpointClamp(endpoint, v[0], v[0], v[0], v[2], v[1], v[1], v[1], v[2] + v[3]);
                    break;
                case 6:
                    SetEndpoint(endpoint, v[0] * v[3] >> 8, v[1] * v[3] >> 8, v[2] * v[3] >> 8, 255, v[0], v[1], v[2], 255);
                    break;
                case 8:
                    if (v[0] + v[2] + v[4] <= v[1] + v[3] + v[5])
                    {
                        SetEndpoint(endpoint, v[0], v[2], v[4], 255, v[1], v[3], v[5], 255);
                    }
                    else
                    {
                        SetEndpointBlue(endpoint, v[1], v[3], v[5], 255, v[0], v[2], v[4], 255);
                    }
                    break;
                case 9:
                    BitTransferSigned(ref v[1], ref v[0]);
                    BitTransferSigned(ref v[3], ref v[2]);
                    BitTransferSigned(ref v[5], ref v[4]);
                    if (v[1] + v[3] + v[5] >= 0)
                    {
                        SetEndpointClamp(endpoint, v[0], v[2], v[4], 255, v[0] + v[1], v[2] + v[3], v[4] + v[5], 255);
                    }
                    else
                    {
                        SetEndpointBlueClamp(endpoint, v[0] + v[1], v[2] + v[3], v[4] + v[5], 255, v[0], v[2], v[4], 255);
                    }
                    break;
                case 10:
                    SetEndpoint(endpoint, v[0] * v[3] >> 8, v[1] * v[3] >> 8, v[2] * v[3] >> 8, v[4], v[0], v[1], v[2], v[5]);
                    break;
                case 12:
                    if (v[0] + v[2] + v[4] <= v[1] + v[3] + v[5])
                    {
                        SetEndpoint(endpoint, v[0], v[2], v[4], v[6], v[1], v[3], v[5], v[7]);
                    }
                    else
                    {
                        SetEndpointBlue(endpoint, v[1], v[3], v[5], v[7], v[0], v[2], v[4], v[6]);
                    }
                    break;
                case 13:
                    BitTransferSigned(ref v[1], ref v[0]);
                    BitTransferSigned(ref v[3], ref v[2]);
                    BitTransferSigned(ref v[5], ref v[4]);
                    BitTransferSigned(ref v[7], ref v[6]);
                    if (v[1] + v[3] + v[5] >= 0)
                    {
                        SetEndpointClamp(endpoint, v[0], v[2], v[4], v[6], v[0] + v[1], v[2] + v[3], v[4] + v[5], v[6] + v[7]);
                    }
                    else
                    {
                        SetEndpointBlueClamp(endpoint, v[0] + v[1], v[2] + v[3], v[4] + v[5], v[6] + v[7], v[0], v[2], v[4], v[6]);
                    }
                    break;
            }
        }
    }

    private static void DecodeWeights(ReadOnlySpan<byte> input, ref BlockData block)
    {
        Span<IntSeqData> wSeq = stackalloc IntSeqData[128];
        Span<byte> texelWeightData = stackalloc byte[16];
        var weightBytes = (block.WeightBits.Align(8) >> 3) & 0xF;
        for (int i = 0; i < weightBytes; i++)
        {
            texelWeightData[i] = BitReverseTable[input[15 - i]];
        }

        var weightTableA = WeightPrecTableA[block.WeightRange];
        var weightTableB = WeightPrecTableB[block.WeightRange];
        DecodeIntseq(BinaryPrimitives.ReadUInt128LittleEndian(texelWeightData), 0, weightTableA, weightTableB, block.WeightNum, wSeq);

        var weightNum = block.WeightNum & 127;
        Span<int> wv = stackalloc int[128];
        if (weightTableA == 0)
        {
            switch (weightTableB)
            {
                case 1:
                    for (int i = 0; i < weightNum; i++)
                    {
                        wv[i] = wSeq[i].Bits != 0 ? 63 : 0;
                    }
                    break;
                case 2:
                    for (int i = 0; i < weightNum; i++)
                    {
                        wv[i] = wSeq[i].Bits << 4 | wSeq[i].Bits << 2 | wSeq[i].Bits;
                    }
                    break;
                case 3:
                    for (int i = 0; i < weightNum; i++)
                    {
                        wv[i] = wSeq[i].Bits << 3 | wSeq[i].Bits;
                    }
                    break;
                case 4:
                    for (int i = 0; i < weightNum; i++)
                    {
                        wv[i] = wSeq[i].Bits << 2 | wSeq[i].Bits >> 2;
                    }
                    break;
                case 5:
                    for (int i = 0; i < weightNum; i++)
                    {
                        wv[i] = wSeq[i].Bits << 1 | wSeq[i].Bits >> 4;
                    }
                    break;
            }
            for (int i = 0; i < weightNum; i++)
            {
                wv[i] += (wv[i] - 33) >>> 31 ^ 1;
            }
        }
        else if (weightTableB == 0)
        {
            int s = weightTableA == 3 ? 5 : 4;
            for (int i = 0; i < weightNum; i++)
            {
                wv[i] = wSeq[i].Nonbits << s;
            }
        }
        else
        {
            if (weightTableA == 3)
            {
                switch (weightTableB)
                {
                    case 1:
                        for (int i = 0; i < weightNum; i++)
                        {
                            wv[i] = wSeq[i].Nonbits * 50;
                        }
                        break;
                    case 2:
                        for (int i = 0; i < weightNum; i++)
                        {
                            wv[i] = wSeq[i].Nonbits * 23;
                            wv[i] += -((wSeq[i].Bits >> 1) & 1) & 0b1000101;
                        }
                        break;
                    case 3:
                        for (int i = 0; i < weightNum; i++)
                        {
                            wv[i] = wSeq[i].Nonbits * 11 + ((wSeq[i].Bits << 4 | wSeq[i].Bits >> 1) & 0b1100011);
                        }
                        break;
                }
            }
            else if (weightTableA == 5)
            {
                switch (weightTableB)
                {
                    case 1:
                        for (int i = 0; i < weightNum; i++)
                        {
                            wv[i] = wSeq[i].Nonbits * 28;
                        }
                        break;
                    case 2:
                        for (int i = 0; i < weightNum; i++)
                        {
                            wv[i] = wSeq[i].Nonbits * 13;
                            wv[i] += -((wSeq[i].Bits >> 1) & 1) & 0b1000010;
                        }
                        break;
                }
            }
            for (int i = 0; i < weightNum; i++)
            {
                int a = -(wSeq[i].Bits & 1) & 0x7F;
                wv[i] = (a & 0x20) | ((wv[i] ^ a) >> 2);
                wv[i] += (wv[i] - 33) >>> 31 ^ 1;
            }
        }

        int ds = (1024 + block.BlockWidth / 2) / (block.BlockWidth - 1);
        int dt = (1024 + block.BlockHeight / 2) / (block.BlockHeight - 1);
        var i0 = block.Width;
        var sMult = ds * (block.Width - 1);
        var tMult= dt * (block.Height - 1);
        if (block.DualPlane == 0)
        {
            var gt0 = 32;
            for (int t = 0, i = 0; t < block.BlockHeight; t++)
            {
                int gt = gt0 >> 6;
                gt0 += tMult;
                int ft = gt & 0xf;
                var v0 = (gt >> 4) * block.Width;
                int gs0 = 32;
                for (int s = 0; s < block.BlockWidth; s++, i++)
                {
                    var gs = gs0 >> 6;
                    gs0 += sMult;
                    int fs = gs & 0xf;
                    int v = (gs >> 4) + v0;
                    int w11 = (fs * ft + 8) >> 4;
                    int w10 = ft - w11;
                    int w01 = fs - w11;
                    int w00 = 16 - fs - ft + w11;

                    block.Weights[i] = (wv[i0 + v + 1] * w11 + wv[i0 + v] * w10 + wv[v + 1] * w01 + wv[v] * w00 + 8) >> 4;
                }
            }
        }
        else
        {
            i0 *= 2;
            for (int t = 0, i = 0; t < block.BlockHeight; t++)
            {
                int gt = (tMult * t + 32) >> 6;
                int ft = gt & 0xf;
                var v0 = (gt >> 4) * block.Width;
                int gs0 = 32;
                for (int s = 0; s < block.BlockWidth; s++, i++)
                {
                    var gs = gs0 >> 6;
                    gs0 += sMult;
                    int fs = gs & 0xf;
                    int v = (gs >> 4) + v0;
                    int w11 = (fs * ft + 8) >> 4;
                    int w10 = ft - w11;
                    int w01 = fs - w11;
                    int w00 = 16 - fs - ft + w11;

                    var i1 = v * 2;
                    block.Weights[i] = (wv[i0 + i1 + 2] * w11 + wv[i0 + i1] * w10 + wv[i1 + 2] * w01 + wv[i1] * w00 + 8) >> 4;
                    block.Weights[i + 144] = (wv[i0 + i1 + 3] * w11 + wv[i0 + i1 + 1] * w10 + wv[i1 + 3] * w01 + wv[i1 + 1] * w00 + 8) >> 4;
                }
            }
        }
    }

    private static void SelectPartition(uint input, ref BlockData block)
    {
        bool smallBlock = block.BlockWidth * block.BlockHeight < 31;
        int seed = (int)(input >> 13 & 0x3ff) | (block.PartNum - 1) << 10;

        uint rnum;
        unchecked
        {
            rnum = (uint)seed;
            rnum ^= rnum >> 15;
            rnum -= rnum << 17;
            rnum += rnum << 7;
            rnum += rnum << 4;
            rnum ^= rnum >> 5;
            rnum += rnum << 16;
            rnum ^= rnum >> 7;
            rnum ^= rnum >> 3;
            rnum ^= rnum << 6;
            rnum ^= rnum >> 17;
        }

        Span<int> seeds = stackalloc int[8];
        for (int i = 0; i < 8; i++)
        {
            seeds[i] = (int)((rnum >> (i * 4)) & 0xF);
            seeds[i] *= seeds[i];
        }

        ReadOnlySpan<int> sh =
        [
            (seed & 2) != 0 ? 4 : 5,
            block.PartNum == 3 ? 6 : 5
        ];

        if ((seed & 1) != 0)
        {
            for (int i = 0; i < 8; i++)
            {
                seeds[i] >>= sh[i % 2];
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                seeds[i] >>= sh[1 - i % 2];
            }
        }

        if (smallBlock)
        {
            for (int t = 0, i = 0; t < block.BlockHeight; t++)
            {
                for (int s = 0; s < block.BlockWidth; s++, i++)
                {
                    int x = s << 1;
                    int y = t << 1;
                    int a = (int)((seeds[0] * x + seeds[1] * y + (rnum >> 14)) & 0x3f);
                    int b = (int)((seeds[2] * x + seeds[3] * y + (rnum >> 10)) & 0x3f);
                    int c = (int)(block.PartNum < 3 ? 0 : (seeds[4] * x + seeds[5] * y + (rnum >> 6)) & 0x3f);
                    int d = (int)(block.PartNum < 4 ? 0 : (seeds[6] * x + seeds[7] * y + (rnum >> 2)) & 0x3f);
                    block.Partition[i] = (a >= b && a >= c && a >= d) ? 0 : (b >= c && b >= d) ? 1 : (c >= d) ? 2 : 3;
                }
            }
        }
        else
        {
            for (int y = 0, i = 0; y < block.BlockHeight; y++)
            {
                for (int x = 0; x < block.BlockWidth; x++, i++)
                {
                    int a = (int)((seeds[0] * x + seeds[1] * y + (rnum >> 14)) & 0x3f);
                    int b = (int)((seeds[2] * x + seeds[3] * y + (rnum >> 10)) & 0x3f);
                    int c = (int)(block.PartNum < 3 ? 0 : (seeds[4] * x + seeds[5] * y + (rnum >> 6)) & 0x3f);
                    int d = (int)(block.PartNum < 4 ? 0 : (seeds[6] * x + seeds[7] * y + (rnum >> 2)) & 0x3f);
                    block.Partition[i] = (a >= b && a >= c && a >= d) ? 0 : (b >= c && b >= d) ? 1 : (c >= d) ? 2 : 3;
                }
            }
        }
    }

    private static void ApplicateColor(BlockData block, Span<uint> output)
    {
        var texelCount = block.BlockWidth * block.BlockHeight;

        Span<EndpointBuffer.Endpoint> endpoints = block.Endpoints[..4];

        // expand and prepare for fast interpolate
        for (var j = 0; j < block.PartNum ; j++)
        {
            Span<int> e = endpoints[j][..8];
            e[0] = (e[0] << 8 | e[0]);
            e[1] = (e[1] << 8 | e[1]);
            e[2] = (e[2] << 8 | e[2]);
            e[3] = (e[3] << 8 | e[3]);
            e[4] = (e[4] << 8 | e[4]) - e[0];
            e[5] = (e[5] << 8 | e[5]) - e[1];
            e[6] = (e[6] << 8 | e[6]) - e[2];
            e[7] = (e[7] << 8 | e[7]) - e[3];
            e[0] = (e[0] << 6) + 32;
            e[1] = (e[1] << 6) + 32;
            e[2] = (e[2] << 6) + 32;
            e[3] = (e[3] << 6) + 32;
        }

        output = output[..texelCount];
        if (block.DualPlane != 0)
        {
            Span<int> ps = [0, 0, 0, 0];
            ps[block.PlaneSelector] = 144;
            ReadOnlySpan<int> weights = block.Weights[..288];
            if (block.PartNum > 1)
            {
                ReadOnlySpan<int> partition = block.Partition[..texelCount];
                for (int i = 0; i < texelCount; i++)
                {
                    ReadOnlySpan<int> endpoint = endpoints[partition[i] & 3][..8];
                    uint r = FastInterpolate(endpoint[0], endpoint[4], weights[i + ps[0]]);
                    uint g = FastInterpolate(endpoint[1], endpoint[5], weights[i + ps[1]]);
                    uint b = FastInterpolate(endpoint[2], endpoint[6], weights[i + ps[2]]);
                    uint a = FastInterpolate(endpoint[3], endpoint[7], weights[i + ps[3]]);
                    output[i] = r | g << 8 | b << 16 | a << 24;
                }
            }
            else
            {
                ReadOnlySpan<int> endpoint = endpoints[0][..8];
                for (int i = 0; i < texelCount; i++)
                {
                    uint r = FastInterpolate(endpoint[0], endpoint[4], weights[i + ps[0]]);
                    uint g = FastInterpolate(endpoint[1], endpoint[5], weights[i + ps[1]]);
                    uint b = FastInterpolate(endpoint[2], endpoint[6], weights[i + ps[2]]);
                    uint a = FastInterpolate(endpoint[3], endpoint[7], weights[i + ps[3]]);
                    output[i] = r | g << 8 | b << 16 | a << 24;
                }
            }
        }
        else if (block.PartNum > 1)
        {
            ReadOnlySpan<int> partition = block.Partition[..texelCount];
            ReadOnlySpan<int> weights = block.Weights[..texelCount];
            for (int i = 0; i < texelCount; i++)
            {
                ReadOnlySpan<int> endpoint = endpoints[partition[i] & 3][..8];
                uint r = FastInterpolate(endpoint[0], endpoint[4], weights[i]);
                uint g = FastInterpolate(endpoint[1], endpoint[5], weights[i]);
                uint b = FastInterpolate(endpoint[2], endpoint[6], weights[i]);
                uint a = FastInterpolate(endpoint[3], endpoint[7], weights[i]);
                output[i] = r | g << 8 | b << 16 | a << 24;
            }
        }
        else
        {
            ReadOnlySpan<int> endpoint = endpoints[0][..8];
            ReadOnlySpan<int> weights = block.Weights[..texelCount];
            for (int i = 0; i < texelCount; i++)
            {
                uint r = FastInterpolate(endpoint[0], endpoint[4], weights[i]);
                uint g = FastInterpolate(endpoint[1], endpoint[5], weights[i]);
                uint b = FastInterpolate(endpoint[2], endpoint[6], weights[i]);
                uint a = FastInterpolate(endpoint[3], endpoint[7], weights[i]);
                output[i] = r | g << 8 | b << 16 | a << 24;
            }
        }
    }

    private static void DecodeIntseq(UInt128 input, int offset, int a, int b, int count, Span<IntSeqData> output)
    {
        if (count <= 0)
        {
            return;
        }

        int n = 0;
        output = output[..count];
        int mask = (1 << b) - 1;
        if (a == 3)
        {
            int blockCount = (count + 4) / 5;
            int lastBlockCount = (count + 4) % 5 + 1;
            int blockSize = 8 + 5 * b;
            int lastBlockSize = (blockSize * lastBlockCount + 4) / 5;

            for (int i = 0, p = offset; i < blockCount; i++, p += blockSize)
            {
                ulong d = GetBits64(input, p, (i < blockCount - 1) ? blockSize : lastBlockSize);
                int x = (int)((d >> b & 3) | (d >> b * 2 & 0xc) | (d >> b * 3 & 0x10) | (d >> b * 4 & 0x60) | (d >> b * 5 & 0x80));
                for (int j = 0; j < 5 && n < count; j++, n++)
                {
                    output[n] = new IntSeqData()
                    {
                        Bits = unchecked((int)(d >> (DImt[j] + b * j))) & mask,
                        Nonbits = DITritsTable[j * 256 + x],
                    };
                }
            }
        }
        else if (a == 5)
        {
            int blockCount = (count + 2) / 3;
            int lastBlockCount = (count + 2) % 3 + 1;
            int blockSize = 7 + 3 * b;
            int lastBlockSize = (blockSize * lastBlockCount + 2) / 3;

            for (int i = 0, p = offset; i < blockCount; i++, p += blockSize)
            {
                ulong d = GetBits64(input, p, (i < blockCount - 1) ? blockSize : lastBlockSize);
                int x = (int)((d >> b & 7) | (d >> b * 2 & 0x18) | (d >> b * 3 & 0x60));
                for (int j = 0; j < 3 && n < count; j++, n++)
                {
                    output[n] = new IntSeqData()
                    {
                        Bits = (int)d >> (DImq[j] + b * j) & mask,
                        Nonbits = DIQuintsTable[j * 128 + x],
                    };
                }
            }
        }
        else
        {
            for (int p = offset; n < count; n++, p += b)
            {
                output[n] = new IntSeqData()
                {
                    Bits = (int)(input >> p) & mask,
                    Nonbits = 0,
                };
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong GetBits64(UInt128 input, int bit, int len) => (ulong) (input >> bit) & ((1UL << len) - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Clamp(int n)
    {
        return n < 0 ? byte.MinValue : n > 255 ? byte.MaxValue : n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void BitTransferSigned(ref int a, ref int b)
    {
        b = (b >> 1) | (a & 0x80);
        a = (a >> 1) & 0x3f;
        if ((a & 0x20) != 0)
        {
            a -= 0x40;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetEndpoint(Span<int> endpoint, int r1, int g1, int b1, int a1, int r2, int g2, int b2, int a2)
    {
        endpoint[7] = a2;
        endpoint[6] = b2;
        endpoint[5] = g2;
        endpoint[4] = r2;
        endpoint[3] = a1;
        endpoint[2] = b1;
        endpoint[1] = g1;
        endpoint[0] = r1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetEndpointClamp(Span<int> endpoint, int r1, int g1, int b1, int a1, int r2, int g2, int b2, int a2)
    {
        endpoint[7] = Clamp(a2);
        endpoint[6] = Clamp(b2);
        endpoint[5] = Clamp(g2);
        endpoint[4] = Clamp(r2);
        endpoint[3] = Clamp(a1);
        endpoint[2] = Clamp(b1);
        endpoint[1] = Clamp(g1);
        endpoint[0] = Clamp(r1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetEndpointBlue(Span<int> endpoint, int r1, int g1, int b1, int a1, int r2, int g2, int b2, int a2)
    {
        endpoint[7] = a2;
        endpoint[6] = b2;
        endpoint[5] = (g2 + b2) >> 1;
        endpoint[4] = (r2 + b2) >> 1;
        endpoint[3] = a1;
        endpoint[2] = b1;
        endpoint[1] = (g1 + b1) >> 1;
        endpoint[0] = (r1 + b1) >> 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetEndpointBlueClamp(Span<int> endpoint, int r1, int g1, int b1, int a1, int r2, int g2, int b2, int a2)
    {
        endpoint[7] = Clamp(a2);
        endpoint[6] = Clamp(b2);
        endpoint[5] = Clamp((g2 + b2) >> 1);
        endpoint[4] = Clamp((r2 + b2) >> 1);
        endpoint[3] = Clamp(a1);
        endpoint[2] = Clamp(b1);
        endpoint[1] = Clamp((g1 + b1) >> 1);
        endpoint[0] = Clamp((r1 + b1) >> 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint FastInterpolate(int v0, int v1, int weight)
    {
        return (uint)((v0 + v1 * weight) >> 14);
    }
}
