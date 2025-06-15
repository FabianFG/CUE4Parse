using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteUtils;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FNaniteVertex
{
    /// <summary>The position value referenced for reference vertices</summary>
    public FIntVector RawPos;
    /// <summary>The position of the vertex in the 3d world.</summary>
    public FVector Pos;
    /// <summary>The attributes of the vertex.</summary>
    public FNaniteVertexAttributes? Attributes;
    /// <summary>True if the vertex as read as a reference. This exists only for debugging purposes.</summary>
    public bool IsRef = false;

    private static FVector UnpackNormals(uint packed, int bits)
    {
        uint mask = BitFieldMaskU32(bits, 0);
        (float f0, float f1) = (GetBits(packed, bits, 0) * (2.0f / mask) - 1.0f, GetBits(packed, bits, bits) * (2.0f / mask) - 1.0f);
        FVector n = new(f0, f1, 1.0f - Math.Abs(f0) - Math.Abs(f1));
        float t = Math.Clamp(-n.Z, 0.0f, 1.0f);
        n.X += n.X >= 0 ? -t : t;
        n.Y += n.Y >= 0 ? -t : t;
        n.Normalize();
        return n;
    }

    private static FVector UnpackTangentX(FVector tangentZ, uint tangentAngleBits, int numTangentBits)
    {
        bool swapXZ = Math.Abs(tangentZ.Z) > Math.Abs(tangentZ.X);
        if (swapXZ)
        {
            tangentZ = new FVector(tangentZ.Z, tangentZ.Y, tangentZ.X);
        }

        FVector tangentRefX = new FVector(-tangentZ.Y, tangentZ.X, 0.0f);
        FVector tangentRefY = tangentZ ^ tangentRefX;

        float dot = 0.0f;
        dot += tangentRefX.X * tangentRefX.X;
        dot += tangentRefX.Y * tangentRefX.Y;
        float scale = 1.0f / (float)Math.Sqrt(dot);

        float tangentAngle = tangentAngleBits * (float)(2.0 * Math.PI) / (1 << numTangentBits);
        FVector tangentX = tangentRefX * (float)(Math.Cos(tangentAngle) * scale) + tangentRefY * (float)(Math.Sin(tangentAngle) * scale);
        if (swapXZ)
        {
            (tangentX.X, tangentX.Z) = (tangentX.Z, tangentX.X);
        }
        return tangentX;
    }

    private static FVector2D UnpackTexCoord(TIntVector2<uint> packed, FUVRange_Old uvRangeOld)
    {
        FVector2D t = new(packed.X, packed.Y);
        t += new FVector2D(
            packed.X > uvRangeOld.GapStart.X ? uvRangeOld.GapLength.X : 0.0f,
            packed.Y > uvRangeOld.GapStart.Y ? uvRangeOld.GapLength.Y : 0.0f
        );
        float scale = PrecisionScales[uvRangeOld.Precision];
        return (t + new FVector2D(uvRangeOld.Min.X, uvRangeOld.Min.Y)) * scale;
    }

    private static FVector2D UnpackTexCoord(TIntVector2<uint> packed, FUVRange uvRange)
    {
        TIntVector2<uint> GlobalUV = new (uvRange.Min.X + packed.X, uvRange.Min.Y + packed.Y);
        return new FVector2D(DecodeUVFloat(GlobalUV.X, uvRange.NumMantissaBits),
            DecodeUVFloat(GlobalUV.Y, uvRange.NumMantissaBits));
    }

    private static float DecodeUVFloat(uint EncodedValue, uint NumMantissaBits)
    {
        uint ExponentAndMantissaMask = BitFieldMaskU32((int) (NANITE_UV_FLOAT_NUM_EXPONENT_BITS + NumMantissaBits), 0);
        bool bNeg = (EncodedValue <= ExponentAndMantissaMask);
        uint ExponentAndMantissa = (bNeg ? ~EncodedValue : EncodedValue) & ExponentAndMantissaMask;

        float Result = (0x3F000000u + (ExponentAndMantissa << (int) (23 - NumMantissaBits)));
        Result = Math.Min(Result * 2.0f - 1.0f, Result); // Stretch denormals from [0.5,1.0] to [0.0,1.0]

        return bNeg ? -Result : Result;
    }

    internal void ReadPosData(FArchive Ar, long srcBaseAddress, long srcBitOffset, FCluster cluster)
    {
        TIntVector2<uint> packed = new(ReadUnalignedDword(Ar, srcBaseAddress, srcBitOffset), ReadUnalignedDword(Ar, srcBaseAddress, srcBitOffset + 32));

        var x = GetBits(packed.X, (int)cluster.PosBitsX, 0);
        packed.X = BitAlignU32(packed.Y, packed.X, cluster.PosBitsX);
        packed.Y >>= (int)cluster.PosBitsX;

        var y = GetBits(packed.X, (int) cluster.PosBitsY, 0);
        packed.X = BitAlignU32(packed.Y, packed.X, cluster.PosBitsY);

        var z = GetBits(packed.X, (int) cluster.PosBitsZ, 0);

        RawPos = new FIntVector((int) x, (int) y, (int) z);
        Pos = new FVector(x, y, z);
        Pos = (RawPos + cluster.PosStart) * cluster.PosScale;
    }

    internal void ReadAttributeData(FArchive Ar, BitStreamReader bitStreamReader, FCluster cluster)
    {
        Attributes = new FNaniteVertexAttributes();

        // parses normals
        uint normalBits = bitStreamReader.Read(Ar, 2 * (int) cluster.NormalPrecision, 2 * NANITE_MAX_NORMAL_QUANTIZATION_BITS(Ar.Game));
        Attributes.Normal = UnpackNormals(normalBits, (int) cluster.NormalPrecision);

        // parse tangent
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            int numTangentBits = cluster.bHasTangents ? ((int) cluster.TangentPrecision + 1) : 0;
            uint tangentAngleAndSignBits = bitStreamReader.Read(Ar, numTangentBits, NANITE_MAX_TANGENT_QUANTIZATION_BITS + 1);
            if (cluster.bHasTangents)
            {
                bool bTangentYSign = (tangentAngleAndSignBits & (1 << (int) cluster.TangentPrecision)) != 0;
                uint tangentAngleBits = GetBits(tangentAngleAndSignBits, (int) cluster.TangentPrecision, 0);
                FVector tangentX = UnpackTangentX(Attributes.Normal, tangentAngleBits, (int) cluster.TangentPrecision);
                Attributes.TangentXAndSign = new FVector4(tangentX, bTangentYSign ? -1.0f : 1.0f);
            }
            else
            {
                Attributes.TangentXAndSign = new FVector4(0, 0, 0, 0);
            }
        }

        // parse color
        var numComponentBits = cluster.ColorComponentBits;
        var colorDelta = new TIntVector4<uint>(
            bitStreamReader.Read(Ar, numComponentBits.X, NANITE_MAX_COLOR_QUANTIZATION_BITS),
            bitStreamReader.Read(Ar, numComponentBits.Y, NANITE_MAX_COLOR_QUANTIZATION_BITS),
            bitStreamReader.Read(Ar, numComponentBits.Z, NANITE_MAX_COLOR_QUANTIZATION_BITS),
            bitStreamReader.Read(Ar, numComponentBits.W, NANITE_MAX_COLOR_QUANTIZATION_BITS)
        );

        const float scale = 1.0f / 255.0f;
        // should be in the ranges of 0.0f .. 1.0f
        var colorMin = cluster.ColorMin;
        Attributes.Color = new FVector4(
            (colorMin.X + colorDelta.X) * scale,
            (colorMin.Y + colorDelta.Y) * scale,
            (colorMin.Z + colorDelta.Z) * scale,
            (colorMin.W + colorDelta.W) * scale
        );

        // parse tex coords
        for (int texCoordIndex = 0; texCoordIndex < cluster.NumUVs; texCoordIndex++)
        {
            var uvPrec = new TIntVector2<uint>(GetBits(cluster.UVBitOffsets, 4, texCoordIndex * 8),
                GetBits(cluster.UVBitOffsets, 4, texCoordIndex * 8 + 4));
            TIntVector2<uint> UVBits =
                new(bitStreamReader.Read(Ar, (int) uvPrec.X, NANITE_MAX_TEXCOORD_QUANTIZATION_BITS),
                    bitStreamReader.Read(Ar, (int) uvPrec.Y, NANITE_MAX_TEXCOORD_QUANTIZATION_BITS));

            if (Ar.Game >= EGame.GAME_UE5_4)
            {
                Attributes.UVs[texCoordIndex] = UnpackTexCoord(UVBits, cluster.UVRanges[texCoordIndex]);
            }
            else
            {
                Attributes.UVs[texCoordIndex] = UnpackTexCoord(UVBits, cluster.UVRanges_Old[texCoordIndex]);
            }
        }
    }
}
