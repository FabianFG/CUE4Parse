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
    private static FVector ComputeTriangleNormal(FVector InPoint0, FVector InPoint1, FVector InPoint2)
    {
        FVector Normal = (InPoint0 - InPoint1) ^ (InPoint1 - InPoint2);
        Normal.Normalize();
        return Normal;
    }

    // Helper method to sample world position at offset
    private static void SampleWorldPositionAtOffset(out FVector OutPoint, byte[] MipData, int X, int Y, int MipSizeX, FVector InLandscapeGridScale)
    {
        int OffsetBytes = (Y * MipSizeX + X) * 4;
        ushort HeightData = (ushort)((MipData[OffsetBytes + 2] * 256) + MipData[OffsetBytes + 1]);

        // NOTE: Since we are using deltas between points to calculate the normal,
        // we don't care about constant offsets in the position, only relative scales
        OutPoint = new FVector(
            X * InLandscapeGridScale.X,
            Y * InLandscapeGridScale.Y,
            GetLocalHeight(HeightData) * InLandscapeGridScale.Z);
    }

    // LandscapeDataAccess.GetLocalHeight
    public static float GetLocalHeight(ushort height) {
        const float LANDSCAPE_ZSCALE = 1.0f / 128.0f;
        const int MaxValue = 65535;
        const float MidValue = 32768f;
        // Reserved 2 bits for other purpose
        // Most significant bit - Visibility, 0 is visible(default), 1 is invisible
        // 2nd significant bit - Triangle flip, not implemented yet
        return (height - MidValue) * LANDSCAPE_ZSCALE;
    }

    public void DecompressMip(byte[] SourceData, long SourceDataBytes, byte[] DestData, long DestDataBytes, int MipIndex)
    {
        var Mip = Mips[MipIndex];
        if (!Mip.bCompressed)
        {
            // mip is uncompressed, just copy it
            Array.Copy(SourceData, 0, DestData, 0, DestDataBytes);
            return;
        }

        var TotalPixels = Mip.SizeX * Mip.SizeY;
        
        // Validate buffer sizes
        if (SourceDataBytes != (TotalPixels + (Mip.SizeX + Mip.SizeY) * 2 - 4) * 2)
        {
            throw new ArgumentException("Source data buffer has incorrect size");
        }
        
        if (DestDataBytes != TotalPixels * 4)
        {
            throw new ArgumentException("Destination data buffer has incorrect size");
        }

        // Undo Delta Encode of Heights
        ushort LastHeight = 32768;
        for (int PixelIndex = 0; PixelIndex < TotalPixels; PixelIndex++)
        {
            int SourceOffset = PixelIndex * 2;
            ushort DeltaHeight = (ushort)((SourceData[SourceOffset] * 256) + SourceData[SourceOffset + 1]);

            // undo delta
            LastHeight += DeltaHeight;

            // texture data is stored as BGRA, or [normal x, height low bits, height high bits, normal y]
            int DestOffset = PixelIndex * 4;
            DestData[DestOffset + 0] = 128; // Initialize normal X to 128 (neutral value)
            DestData[DestOffset + 1] = (byte)(LastHeight & 0xff); // Height low bits
            DestData[DestOffset + 2] = (byte)(LastHeight >> 8);   // Height high bits
            DestData[DestOffset + 3] = 128; // Initialize normal Y to 128 (neutral value)
        }

        // Recompute normals in the interior
        {
            // Skip computing the edges, as they will be overwritten later
            // This avoids handling samples that go off the edge
            for (int Y = 1; Y < Mip.SizeY - 1; Y++)
            {
                for (int X = 1; X < Mip.SizeX - 1; X++)
                {
                    // Based on shader code in LandscapeLayersPS.usf
                    FVector TL, TT, CC, LL, RR, BR, BB;

                    SampleWorldPositionAtOffset(out TL, DestData, X - 1, Y - 1, Mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out TT, DestData, X + 0, Y - 1, Mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out CC, DestData, X + 0, Y + 0, Mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out LL, DestData, X - 1, Y + 0, Mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out RR, DestData, X + 1, Y + 0, Mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out BR, DestData, X + 1, Y + 1, Mip.SizeX, LandscapeGridScale);
                    SampleWorldPositionAtOffset(out BB, DestData, X + 0, Y + 1, Mip.SizeX, LandscapeGridScale);

                    FVector N0 = ComputeTriangleNormal(CC, LL, TL);
                    FVector N1 = ComputeTriangleNormal(TL, TT, CC);
                    FVector N2 = ComputeTriangleNormal(LL, CC, BB);
                    FVector N3 = ComputeTriangleNormal(RR, CC, TT);
                    FVector N4 = ComputeTriangleNormal(BR, BB, CC);
                    FVector N5 = ComputeTriangleNormal(CC, RR, BR);

                    FVector FinalNormal = (N0 + N1 + N2 + N3 + N4 + N5);
                    FinalNormal.Normalize();

                    // Rescale normal.xy to [0,255] range, and write out as bytes
                    int OffsetBytes = (Y * Mip.SizeX + X) * 4;
                    DestData[OffsetBytes + 0] = (byte)Math.Clamp(((FinalNormal.X + 1.0f) * 0.5f) * 255.0f, 0.0f, 255.0f);
                    DestData[OffsetBytes + 3] = (byte)Math.Clamp(((FinalNormal.Y + 1.0f) * 0.5f) * 255.0f, 0.0f, 255.0f);
                }
            }
        }

        // Write out normals along the edge (delta encoded clockwise starting from top left)
        int SourceOffset2 = TotalPixels * 2;
        byte LastNormalX = 128;
        byte LastNormalY = 128;

        // Define a local function to decode normals
        void DecodeNormal(int X, int Y)
        {
            int DestOffset = (Y * Mip.SizeX + X) * 4;
            LastNormalX += SourceData[SourceOffset2 + 0];
            LastNormalY += SourceData[SourceOffset2 + 1];
            DestData[DestOffset + 0] = LastNormalX;
            DestData[DestOffset + 3] = LastNormalY;
            SourceOffset2 += 2;
        }

        // Top edge: [0 ... MipSizeX-1], 0
        for (int X = 0; X < Mip.SizeX; X++)
        {
            DecodeNormal(X, 0);
        }

        // Right edge: MipSizeX-1, [1 ... MipSizeY-1]
        for (int Y = 1; Y < Mip.SizeY; Y++)
        {
            DecodeNormal(Mip.SizeX - 1, Y);
        }

        // Bottom edge: [MipSizeX-2 ... 0], MipSizeY-1
        for (int X = Mip.SizeX - 2; X >= 0; X--)
        {
            DecodeNormal(X, Mip.SizeY - 1);
        }

        // Left edge: 0, [MipSizeY-2 ... 1]
        for (int Y = Mip.SizeY - 2; Y >= 1; Y--)
        {
            DecodeNormal(0, Y);
        }

        // Verify we've consumed all source data
        if (SourceOffset2 != SourceDataBytes)
        {
            throw new InvalidOperationException($"Did not consume all source data. Consumed {SourceOffset2} bytes out of {SourceDataBytes}");
        }
    }
}
