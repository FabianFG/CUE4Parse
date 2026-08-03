using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeTextureStorageProviderFactory : UTextureAllMipDataProviderFactory
{
    public int BoundaryOffset { get; private set; }
    public int BoundaryCountX { get; private set; }
    public int BoundaryCountY { get; private set; }
    public int NumNonOptionalMips { get; private set; }
    public int NumNonStreamingMips { get; private set; }
    public FVector LandscapeGridScale { get; private set; }
    public FLandscapeTexture2DMipMap[] Mips { get; private set; }
    public FPackageIndex Texture { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        BoundaryOffset = GetOrDefault<int>(nameof(BoundaryOffset));
        BoundaryCountX = GetOrDefault<int>(nameof(BoundaryCountX));
        BoundaryCountY = GetOrDefault<int>(nameof(BoundaryCountY));

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

        var destWidth = mip.SizeX;
        var destHeight = mip.SizeY;

        // If the texture is shared, or there are subsections, need to add back a duplicate row/column at every BoundaryOffset
        var mipBoundaryOffset = BoundaryOffset >> mipIndex;
        // High index mips drop the row/column duplication at low resolutions. Offset is only relevant when duplication occurs at least after every other pixel (Offset >= 2)
        var bHasDuplicateData = (BoundaryCountX > 0 || BoundaryCountY > 0) && mipBoundaryOffset > 1;
        var mipBoundaryCountX = bHasDuplicateData ? BoundaryCountX : 0;
        var mipBoundaryCountY = bHasDuplicateData ? BoundaryCountY : 0;
        Debug.Assert(!bHasDuplicateData || MathUtils.IsPowerOfTwo(mipBoundaryOffset));

        var srcWidth = destWidth - mipBoundaryCountX;
        var srcHeight = destHeight - mipBoundaryCountY;
        Debug.Assert(srcHeight >= 0 && srcWidth >= 0);

        int numSrcPixels = srcWidth * srcHeight;
        Debug.Assert(sourceDataBytes == (numSrcPixels + (destWidth + destHeight) * 2 - 4) * 2); // 2 bytes (height) for each pixel, plus 2 bytes (normal x/y) for each border pixel
        Debug.Assert(destDataBytes == destWidth * destHeight * 4);

        // Save some multiplying by premultiplying the grid scales, mip scale and ZScale
        var premultU16 = CalculatePremultU16(mipIndex, LandscapeGridScale);

        // Current center pixel height
        // (also used to delta decode the heights - initial value must match the initial value used during encoding)
        ushort cc = 32768;

        // Partial normal results recorded for the previous line
        var prevLinePartialNormals = new FVector[destWidth];
        // FVector is a struct, the array is already initialized to 0
        // for (int i = 0; i < prevLinePartialNormals.Length; i++)
        // {
        //     prevLinePartialNormals[i] = new FVector(0, 0, 0);
        // }

        fixed (byte* srcPtr = sourceData)
        fixed (byte* dstPtr = destData)
        {
            var duplicateRowsSkipped = 0;
            var previousRowOffset = 1;

            // Iterate each line
            for (int y = 0; y < destHeight; y++)
            {
                // Offset the src since the data has removed all of the duplicate rows
                var srcLineOffsetInPixels = (y - duplicateRowsSkipped) * srcWidth;
                var destLineOffsetInPixels = y * destWidth;
                byte* src = &srcPtr[srcLineOffsetInPixels * 2];
                FColor* dst = (FColor*)&dstPtr[destLineOffsetInPixels * 4];

                if (y == 0)
                {
                    // Just decode heights for the first line (normals don't matter they will be stomped below)
                    for (int x = 0; x < destWidth; x++)
                    {
                        // Skip the duplicate column. Duplicate columns will be copied once at the end of function
                        if (bHasDuplicateData && IsDuplicateCoord(x, mipBoundaryOffset))
                        {
                            dst++;
                            continue;
                        }

                        ushort deltaHeight = (ushort)(src[0] * 256 + src[1]);
                        cc += deltaHeight;
                        *dst = new FColor((byte)(cc >> 8), (byte)(cc & 0xff), 128, 128);
                        src += 2;
                        dst++;
                    }
                }
                else
                {
                    // Duplicate rows will be copied once at the end of function
                    if (bHasDuplicateData && IsDuplicateCoord(y, mipBoundaryOffset))
                    {
                        duplicateRowsSkipped++;
                        previousRowOffset = 2;
                        continue;
                    }

                    // Duplicate rows are not considered in the normal calculation
                    // The top pixel is two rows above after skipping a duplicate row
                    var ttOffset = destWidth * previousRowOffset;

                    // compute initial values (first pixel)
                    // previous quad N1 and (N0+N1) normals
                    var p1 = FVector.ZeroVector;
                    var p01 = FVector.ZeroVector;
                    ushort tt;												// previous quad TT height
                    {
                        ushort deltaHeight = (ushort)(src[0] * 256 + src[1]);
                        cc += deltaHeight;
                        *dst = new FColor((byte)(cc >> 8), (byte)(cc & 0xff), 128, 128);

                        // load TT for first pixel (becomes TL for second pixel)
                        tt = DecodeHeightU16(dst - ttOffset);

                        src += 2;
                        dst++;
                    }

                    var prevColumnOffset = -1;
                    // Rest of the pixels in the line
                    for (int x = 1; x < destWidth; x++)
                    {
                        // Duplicate column data is copied once at the end of function
                        if (bHasDuplicateData && IsDuplicateCoord(x, mipBoundaryOffset))
                        {
                            dst++;
                            prevColumnOffset = -2;
                            continue;
                        }

                        // Re-use previous pixel TT and CC as this pixel TL and LL
                        ushort tl = tt;
                        ushort ll = cc;

                        // 1) Decode Height at CC
                        ushort deltaHeight = (ushort)(src[0] * 256 + src[1]);
                        cc += deltaHeight;

                        // Load TT
                        tt = DecodeHeightU16(dst - ttOffset);

                        // 2) Write Height at CC (normals get written during processing of the next line)
                        *dst = new FColor((byte)(cc >> 8), (byte)(cc & 0xff), 128, 128);

                        // 3) Compute local normals N0/N1 for the current quad (CC/TT/TL/LL)
                        var n0 = ComputeGridNormalFromDeltaHeightsPremultU16(cc - ll, ll - tl, premultU16);
                        var n1 = ComputeGridNormalFromDeltaHeightsPremultU16(tt - tl, cc - tt, premultU16);
                        var n01 = n0 + n1;

                        // 4) Complete Normal calculation for TL - this takes the partial result from the previous line and fills in the rest
                        var tlNormal = prevLinePartialNormals[x + prevColumnOffset] + p1 + n01;
                        FastNormalize(ref tlNormal);

                        // 5) Write Normal for TL
                        dst[-ttOffset + prevColumnOffset].B = (byte)Math.Clamp(tlNormal.X * 127.5f + 127.5f, 0.0f, 255.0f);
                        dst[-ttOffset + prevColumnOffset].A = (byte)Math.Clamp(tlNormal.Y * 127.5f + 127.5f, 0.0f, 255.0f);

                        // 6) Store Partial Normal for LL in PrevLinePartialNormals (P0 + P1 + N0) - the rest will be filled in when processing the next line
                        var llPartialNormal = p01 + n0;
                        prevLinePartialNormals[x + prevColumnOffset] = llPartialNormal;

                        // pass normals to next pixel
                        p1 = n1;
                        p01 = n01;

                        src += 2;
                        dst++;
                        prevColumnOffset = -1;
                    }
                }
            }

            // Write out normals along the edge (delta encoded clockwise starting from top left)
            {
                byte* src = &srcPtr[numSrcPixels * 2];
                byte lastNormalX = 128;
                byte lastNormalY = 128;

                void DecodeNormal(int x, int y, byte* dst)
                {
                    var destOffset = (y * destWidth + x) * 4;
                    lastNormalX += src[0];
                    lastNormalY += src[1];
                    dst[destOffset + 0] = lastNormalX;
                    dst[destOffset + 3] = lastNormalY;
                    src += 2;
                }

                for (var x = 0; x < destWidth; x++)		// [0 ... Width-1], 0
                {
                    DecodeNormal(x, 0, dstPtr);
                }

                for (var y = 1; y < destHeight; y++)		// Width-1, [1 ... Height-1]
                {
                    DecodeNormal(destWidth - 1, y, dstPtr);
                }

                for (var x = destWidth - 2; x >= 0; x--)	// [Width-2 ... 0], Height-1
                {
                    DecodeNormal(x, destHeight - 1, dstPtr);
                }

                for (var y = destHeight - 2; y >= 1; y--)	// 0, [Height-2 ... 1]
                {
                    DecodeNormal(0, y, dstPtr);
                }
                Debug.Assert(src == &srcPtr[sourceDataBytes]);

                // Copy all duplicate row/column data once all height/normal data is set
                if (bHasDuplicateData)
                {
                    // For each duplicate row, copy the pixels above
                    for (var y = mipBoundaryOffset; y < destHeight; y += mipBoundaryOffset)
                    {
                        Debug.Assert(IsDuplicateCoord(y, mipBoundaryOffset));
                        FColor* dst = (FColor*)&dstPtr[y * destWidth * 4];

                        for (var x = 0; x < destWidth; x++)
                        {
                            *dst = dst[-destWidth];
                            dst++;
                        }
                    }

                    // For each duplicate column, copy the pixels to the left
                    for (var x = mipBoundaryOffset; x < destWidth; x += mipBoundaryOffset)
                    {
                        Debug.Assert(IsDuplicateCoord(x, mipBoundaryOffset));
                        FColor* dst = (FColor*)&dstPtr[x * 4];
                        for (var y = 0; y < destHeight; y++)
                        {
                            *dst = dst[-1];
                            dst += destWidth;
                        }
                    }
                }
            }
        }
    }

    private bool IsDuplicateCoord(int xy, int mipBoundaryOffset)
    {
        Debug.Assert(MathUtils.IsPowerOfTwo(mipBoundaryOffset));
        return xy != 0 && (xy & (mipBoundaryOffset - 1)) == 0;
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
