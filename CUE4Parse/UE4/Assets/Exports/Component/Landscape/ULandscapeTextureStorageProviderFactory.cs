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

    const float LANDSCAPE_ZSCALE = 1.0f / 128.0f;
    const float MidValue = 32768f;

    // LandscapeDataAccess.GetLocalHeight

    private FVector2D CalculatePremultU16(int mipIndex, FVector gridScale)
    {
        int mipScale = 1 << mipIndex;
        
        float scaleFactor = -LANDSCAPE_ZSCALE / (gridScale.X * gridScale.Y * mipScale);

        var x = gridScale.Z * gridScale.Y * scaleFactor;
        var y = gridScale.Z * gridScale.X * scaleFactor;
        return new FVector2D(x, y);
    }

    public unsafe void DecompressMip(byte[] sourceData, long sourceDataBytes, byte[] destData, long destDataBytes, int mipIndex)
    {
        // Check if the mip is not compressed, just copy it
        FLandscapeTexture2DMipMap mip = Mips[mipIndex];
        if (!mip.bCompressed)
        {
            Array.Copy(sourceData, 0, destData, 0, destDataBytes);
            return;
        }

        int width = mip.SizeX;
        int height = mip.SizeY;
        int totalPixels = width * height;
        int borderPixels = (width + height) * 2 - 4;
        
        if (sourceDataBytes != (totalPixels + borderPixels) * 2) 
            throw new InvalidOperationException("Invalid source data size");
        if (destDataBytes != totalPixels * 4) 
            throw new InvalidOperationException("Invalid destination data size");

        // Save some multiplying by premultiplying the grid scales, mip scale and ZScale
        FVector2D premultU16 = CalculatePremultU16(mipIndex, LandscapeGridScale);

        // Current center pixel height
        // (also used to delta decode the heights - initial value must match the initial value used during encoding)
        ushort CC = 32768;

        // Partial normal results recorded for the previous line
        FVector[] prevLinePartialNormals = new FVector[width];
        for (int i = 0; i < width; i++)
            prevLinePartialNormals[i] = new FVector(0, 0, 0);

        fixed (byte* srcPtr = sourceData)
        fixed (byte* dstPtr = destData)
        {
            // Iterate each line
            for (int y = 0; y < height; y++)
            {
                int lineOffsetInPixels = y * width;
                byte* src = srcPtr + (lineOffsetInPixels * 2);
                FColor* dstColorPtr = (FColor*)(dstPtr + lineOffsetInPixels * 4);

                if (y == 0)
                {
                    // Just decode heights for the first line (normals don't matter they will be stomped below)
                    for (int x = 0; x < width; x++)
                    {
                        ushort deltaHeight = (ushort)(src[0] * 256 + src[1]);
                        CC += deltaHeight;
                        *dstColorPtr = new FColor((byte)(CC >> 8), (byte)(CC & 0xff), 128, 128);
                        src += 2;
                        dstColorPtr++;
                    }
                }
                else
                {
                    // Compute initial values (first pixel)
                    FVector P1 = new FVector(0, 0, 0);  // previous quad N1 normal
                    FVector P01 = new FVector(0, 0, 0); // previous quad (N0+N1) normals
                    ushort TT;                              // previous quad TT height
                    {
                        ushort deltaHeight = (ushort)(src[0] * 256 + src[1]);
                        CC += deltaHeight;
                        *dstColorPtr = new FColor((byte)(CC >> 8), (byte)(CC & 0xff), 128, 128);

                        // Load TT for first pixel (becomes TL for second pixel)
                        TT = DecodeHeightU16(dstColorPtr + 0 - width);

                        src += 2;
                        dstColorPtr++;
                    }

                    // Rest of the pixels in the line
                    for (int x = 1; x < width; x++)
                    {
                        // Re-use previous pixel TT and CC as this pixel TL and LL
                        ushort TL = TT;
                        ushort LL = CC;

                        // 1) Decode Height at CC
                        ushort deltaHeight = (ushort)(src[0] * 256 + src[1]);
                        CC += deltaHeight;

                        // Load TT
                        TT = DecodeHeightU16(dstColorPtr + 0 - width);

                        // 2) Write Height at CC (normals get written during processing of the next line)
                        *dstColorPtr = new FColor((byte)(CC >> 8), (byte)(CC & 0xff), 128, 128);
                        
                        // 3) Compute local normals N0/N1 for the current quad (CC/TT/TL/LL)
                        FVector N0 = ComputeGridNormalFromDeltaHeightsPremultU16(CC - LL, LL - TL, premultU16);
                        FVector N1 = ComputeGridNormalFromDeltaHeightsPremultU16(TT - TL, CC - TT, premultU16);
                        FVector N01 = N0 + N1;


                        // 4) Complete Normal calculation for TL - this takes the partial result from the previous line and fills in the rest
                        FVector TL_Normal = prevLinePartialNormals[x - 1] + P1 + N01;
                        FastNormalize(ref TL_Normal);

                        // 5) Write Normal for TL
                        FColor* topLeftPixel = dstColorPtr - width - 1;
                        topLeftPixel->B = (byte)Math.Clamp((TL_Normal.X * 127.5f + 127.5f), 0.0f, 255.0f);
                        topLeftPixel->A = (byte)Math.Clamp((TL_Normal.Y * 127.5f + 127.5f), 0.0f, 255.0f);

                        // 6) Store Partial Normal for LL in PrevLinePartialNormals (P0 + P1 + N0) - the rest will be filled in when processing the next line
                        FVector LL_PartialNormal = P01 + N0;
                        prevLinePartialNormals[x - 1] = LL_PartialNormal;

                        // Pass normals to next pixel
                        P1 = N1;
                        P01 = N01;

                        src += 2;
                        dstColorPtr++;
                    }
                }
            }

            // Write out normals along the edge (delta encoded clockwise starting from top left)
            {
                byte* src = srcPtr + (totalPixels * 2);
                byte lastNormalX = 128;
                byte lastNormalY = 128;

                {
                    void DecodeNormal(int x, int y, byte* dst)
                    {
                        int destOffset = (y * width + x) * 4;
                        lastNormalX += src[0];
                        lastNormalY += src[1];
                        dst[destOffset + 0] = lastNormalX;
                        dst[destOffset + 3] = lastNormalY;
                        src += 2;
                    }
                    
                    for (int x = 0; x < width; x++)      // [0 ... Width-1], 0
                    {
                        DecodeNormal(x, 0, dstPtr);
                    }

                    for (int y = 1; y < height; y++)      // Width-1, [1 ... Height-1]
                    {
                        DecodeNormal(width - 1, y, dstPtr);
                    }

                    for (int x = width - 2; x >= 0; x--)  // [Width-2 ... 0], Height-1
                    {
                        DecodeNormal(x, height - 1, dstPtr);
                    }

                    for (int y = height - 2; y >= 1; y--)  // 0, [Height-2 ... 1]
                    {
                        DecodeNormal(0, y, dstPtr);
                    }
                }
            }
        }
    }

    private FVector ComputeGridNormalFromDeltaHeightsPremultU16(int dhdx, int dhdy, FVector2D premultU16)
    {
        FVector normal = new FVector(
            dhdx * premultU16.X,
            dhdy * premultU16.Y,
            1.0f
        );
        
        // Normalize (optimized)
        float squareSum = normal.X * normal.X + normal.Y * normal.Y + 1.0f;
        if (squareSum > UnrealMath.SmallNumber)
        {
            float scale = 1.0f / (float)Math.Sqrt(squareSum);
            normal.X *= scale;
            normal.Y *= scale;
            normal.Z = scale;
        }
        else
        {
            normal.X = 0.0f;
            normal.Y = 0.0f;
            normal.Z = 1.0f;
        }
        
        return normal;
    }
    
    private static unsafe ushort DecodeHeightU16(FColor* pixel)
    {
        ushort heightData = (ushort)(pixel->R * 256 + pixel->G);
        return heightData;
    }

    private static void FastNormalize(ref FVector v)
    {
        float squareSum = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        if (squareSum > UnrealMath.SmallNumber)
        {
            float scale = 1.0f / MathF.Sqrt(squareSum);
            v.X *= scale;
            v.Y *= scale;
            v.Z *= scale;
        }
        else
        {
            v.X = 0.0f;
            v.Y = 0.0f;
            v.Z = 1.0f;
        }
    }
}
