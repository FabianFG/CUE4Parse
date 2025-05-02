using System;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

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
        // var OptionalMips = Mips.Length - NumNonOptionalMips;
        // check(OptionalMips >= 0);

        // var FirstInlineMip = Mips.Length - NumNonStreamingMips;
        // check(FirstInlineMip >= 0);

        NumNonOptionalMips = Ar.Read<int>();
        NumNonStreamingMips = Ar.Read<int>();
        LandscapeGridScale = Ar.Read<FVector>();

        for (var i = 0; i < Mips.Length; i++)
        {
            // select bulk data flags for optional/streaming/inline mips
            // EBulkDataFlags BulkDataFlags;
            // if (i < OptionalMips)
            // {
            //     // optional mip
            //     BulkDataFlags = EBulkDataFlags.BULKDATA_Force_NOT_InlinePayload | EBulkDataFlags.BULKDATA_OptionalPayload;
            // }
            // else if (i < FirstInlineMip)
            // {
            //     // streaming mip
            //     bool bDuplicateNonOptionalMips = false; // TODO [chris.tchou] : if we add support for optional mips, we might need to calculate this.
            //     BulkDataFlags = EBulkDataFlags.BULKDATA_Force_NOT_InlinePayload | (bDuplicateNonOptionalMips ? EBulkDataFlags.BULKDATA_DuplicateNonOptionalPayload : 0);
            // }
            // else
            // {
            //     // non streaming inline mip (can be single use as we only need to upload to GPU once, are never streamed out)
            //     BulkDataFlags = EBulkDataFlags.BULKDATA_ForceInlinePayload | EBulkDataFlags.BULKDATA_SingleUse;
            // }
            Mips[i] = new FLandscapeTexture2DMipMap(Ar/*, BulkDataFlags*/);
        }

        Texture = new FPackageIndex(Ar);
    }

    public static void ApplyTo(UTexture2D? TargetTexture, ref FVector InLandsapeGridScale)
    {
        if (TargetTexture is null) return;

        var Width = TargetTexture.PlatformData.SizeX;
        var Height = TargetTexture.PlatformData.SizeY;
        var MipCount = TargetTexture.PlatformData.Mips.Length;
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
    }
}
