using System;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeTextureStorageProviderFactory : UTextureAllMipDataProviderFactory
{
    public int NumNonOptionalMips { get; private set; }
    public int NumNonStreamingMips { get; private set; }
    public FVector LandscapeGridScale { get; private set; }
    public FLandscapeTexture2DMipMap[] Mips { get; private set; }
    public FPackageIndex Texture { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        NumNonOptionalMips = Ar.Read<int>();
        NumNonStreamingMips = Ar.Read<int>();
        LandscapeGridScale = new FVector(Ar);

        Mips = Ar.ReadArray(() => new FLandscapeTexture2DMipMap(Ar));

        Texture = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("NumNonOptionalMips");
        writer.WriteValue(NumNonOptionalMips);

        writer.WritePropertyName("NumNonStreamingMips");
        writer.WriteValue(NumNonStreamingMips);

        writer.WritePropertyName("LandscapeGridScale");
        serializer.Serialize(writer, LandscapeGridScale);

        writer.WritePropertyName("Mips");
        serializer.Serialize(writer, Mips);

        writer.WritePropertyName("Texture");
        serializer.Serialize(writer, Texture);
    }

    // Helper method to compute triangle normal
    private static FVector ComputeTriangleNormal(FVector inPoint0, FVector inPoint1, FVector inPoint2)
    {
        FVector normal = (inPoint0 - inPoint1) ^ (inPoint1 - inPoint2);
        normal.Normalize();
        return normal;
    }

    // Helper method to sample world position at offset
    private static void SampleWorldPositionAtOffset(out FVector outPoint, byte[] mipData, int x, int y, int mipSizeX, FVector inLandscapeGridScale)
    {
        int offsetBytes = (y * mipSizeX + x) * 4;
        ushort heightData = (ushort)((mipData[offsetBytes + 2] * 256) + mipData[offsetBytes + 1]);

        // NOTE: Since we are using deltas between points to calculate the normal,
        // we don't care about constant offsets in the position, only relative scales
        outPoint = new FVector(
            x * inLandscapeGridScale.X,
            y * inLandscapeGridScale.Y,
            GetLocalHeight(heightData) * inLandscapeGridScale.Z);
    }

    const float LANDSCAPE_ZSCALE = 1.0f / 128.0f;
    const float MidValue = 32768f;

    // LandscapeDataAccess.GetLocalHeight
    public static float GetLocalHeight(ushort height) 
    {
        // Reserved 2 bits for other purpose
        // Most significant bit - Visibility, 0 is visible(default), 1 is invisible
        // 2nd significant bit - Triangle flip, not implemented yet
        return (height - MidValue) * LANDSCAPE_ZSCALE;
    }

    public void DecompressMip(byte[] sourceData, long sourceDataBytes, byte[] destData, long destDataBytes, int mipIndex)
    {
        var mip = Mips[mipIndex];
        if (!mip.bCompressed)
        {
            // mip is uncompressed, just copy it
            Array.Copy(sourceData, 0, destData, 0, destDataBytes);
            return;
        }

        var totalPixels = mip.SizeX * mip.SizeY;
        
        // Validate buffer sizes
        if (sourceDataBytes != (totalPixels + (mip.SizeX + mip.SizeY) * 2 - 4) * 2)
        {
            throw new ArgumentException("Source data buffer has incorrect size");
        }
        
        if (destDataBytes != totalPixels * 4)
        {
            throw new ArgumentException("Destination data buffer has incorrect size");
        }

        // Undo Delta Encode of Heights
        ushort lastHeight = 32768;
        for (int pixelIndex = 0; pixelIndex < totalPixels; pixelIndex++)
        {
            int sourceOffset = pixelIndex * 2;
            ushort deltaHeight = (ushort)((sourceData[sourceOffset] * 256) + sourceData[sourceOffset + 1]);

            // undo delta
            lastHeight += deltaHeight;

            // texture data is stored as BGRA, or [normal x, height low bits, height high bits, normal y]
            int destOffset = pixelIndex * 4;
            destData[destOffset + 0] = 128; // Initialize normal X to 128 (neutral value)
            destData[destOffset + 1] = (byte)(lastHeight & 0xff); // Height low bits
            destData[destOffset + 2] = (byte)(lastHeight >> 8);   // Height high bits
            destData[destOffset + 3] = 128; // Initialize normal Y to 128 (neutral value)
        }

        // Recompute normals in the interior
        {
            // Skip computing the edges, as they will be overwritten later
            // This avoids handling samples that go off the edge
            for (int y = 1; y < mip.SizeY - 1; y++)
            {
                for (int x = 1; x < mip.SizeX - 1; x++)
                {
                    // Based on shader code in LandscapeLayersPS.usf
                    FVector TL, TT, CC, LL, RR, BR, BB;

                    SampleWorldPositionAtOffset(out TL, destData, x - 1, y - 1, mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out TT, destData, x + 0, y - 1, mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out CC, destData, x + 0, y + 0, mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out LL, destData, x - 1, y + 0, mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out RR, destData, x + 1, y + 0, mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out BR, destData, x + 1, y + 1, mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out BB, destData, x + 0, y + 1, mip.SizeX, LandscapeGridScale);

                    FVector N0 = ComputeTriangleNormal(CC, LL, TL);
                    FVector N1 = ComputeTriangleNormal(TL, TT, CC);
                    FVector N2 = ComputeTriangleNormal(LL, CC, BB);
                    FVector N3 = ComputeTriangleNormal(RR, CC, TT);
                    FVector N4 = ComputeTriangleNormal(BR, BB, CC);
                    FVector N5 = ComputeTriangleNormal(CC, RR, BR);

                    FVector finalNormal = (N0 + N1 + N2 + N3 + N4 + N5);
                    finalNormal.Normalize();

                    // Rescale normal.xy to [0,255] range, and write out as bytes
                    int offsetBytes = (y * mip.SizeX + x) * 4;
                    destData[offsetBytes + 0] = (byte)Math.Clamp(((finalNormal.X + 1.0f) * 0.5f) * 255.0f, 0.0f, 255.0f);
                    destData[offsetBytes + 3] = (byte)Math.Clamp(((finalNormal.Y + 1.0f) * 0.5f) * 255.0f, 0.0f, 255.0f);
                }
            }
        }

        // Write out normals along the edge (delta encoded clockwise starting from top left)
        int sourceOffset2 = totalPixels * 2;
        byte lastNormalX = 128;
        byte lastNormalY = 128;

        // Define a local function to decode normals
        void DecodeNormal(int x, int y)
        {
            int destOffset = (y * mip.SizeX + x) * 4;
            lastNormalX += sourceData[sourceOffset2 + 0];
            lastNormalY += sourceData[sourceOffset2 + 1];
            destData[destOffset + 0] = lastNormalX;
            destData[destOffset + 3] = lastNormalY;
            sourceOffset2 += 2;
        }

        // Top edge: [0 ... MipSizeX-1], 0
        for (int x = 0; x < mip.SizeX; x++)
        {
            DecodeNormal(x, 0);
        }

        // Right edge: MipSizeX-1, [1 ... MipSizeY-1]
        for (int y = 1; y < mip.SizeY; y++)
        {
            DecodeNormal(mip.SizeX - 1, y);
        }

        // Bottom edge: [MipSizeX-2 ... 0], MipSizeY-1
        for (int x = mip.SizeX - 2; x >= 0; x--)
        {
            DecodeNormal(x, mip.SizeY - 1);
        }

        // Left edge: 0, [MipSizeY-2 ... 1]
        for (int y = mip.SizeY - 2; y >= 1; y--)
        {
            DecodeNormal(0, y);
        }

        // Verify we've consumed all source data
        if (sourceOffset2 != sourceDataBytes)
        {
            throw new InvalidOperationException($"Did not consume all source data. Consumed {sourceOffset2} bytes out of {sourceDataBytes}");
        }
    }
}
