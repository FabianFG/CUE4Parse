using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public abstract class UTexture : UUnrealMaterial
{
    public FGuid LightingGuid { get; private set; }
    public TextureCompressionSettings CompressionSettings { get; private set; }
    public bool SRGB { get; private set; }
    public bool RenderNearestNeighbor { get; private set; }
    public EPixelFormat Format { get; protected set; } = EPixelFormat.PF_Unknown;
    public FTexturePlatformData PlatformData { get; private set; } = new();

    public bool IsNormalMap => CompressionSettings == TextureCompressionSettings.TC_Normalmap;
    public bool IsHDR => CompressionSettings is
        TextureCompressionSettings.TC_HDR or
        TextureCompressionSettings.TC_HDR_F32 or
        TextureCompressionSettings.TC_HDR_Compressed or
        TextureCompressionSettings.TC_HalfFloat or
        TextureCompressionSettings.TC_SingleFloat;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        LightingGuid = GetOrDefault(nameof(LightingGuid), new FGuid((uint) GetFullName().GetHashCode()));
        CompressionSettings = GetOrDefault(nameof(CompressionSettings), TextureCompressionSettings.TC_Default);
        SRGB = GetOrDefault(nameof(SRGB), true);

        if (TryGetValue(out FName trigger, "LODGroup", "Filter") && !trigger.IsNone)
        {
            RenderNearestNeighbor = trigger.Text.EndsWith("TEXTUREGROUP_Pixels2D", StringComparison.OrdinalIgnoreCase) ||
                                    trigger.Text.EndsWith("TF_Nearest", StringComparison.OrdinalIgnoreCase);
        }

        var stripFlags = Ar.Read<FStripDataFlags>();

        // If archive is has editor only data
        if (!stripFlags.IsEditorDataStripped())
        {
            // if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.VirtualizedBulkDataHaveUniqueGuids)
            // {
            //
            // }

            // throw new NotImplementedException("Non-Cooked Textures are not supported");
        }
    }

    protected void DeserializeCookedPlatformData(FAssetArchive Ar, bool bSerializeMipData = true)
    {
        var pixelFormatName = Ar.ReadFName();
        while (!pixelFormatName.IsNone)
        {
            Enum.TryParse(pixelFormatName.Text, out EPixelFormat pixelFormat);

            var skipOffset = Ar.Game switch
            {
                >= EGame.GAME_UE5_0 => Ar.AbsolutePosition + Ar.Read<long>(),
                >= EGame.GAME_UE4_20 => Ar.Read<long>(),
                _ => Ar.Read<int>()
            };

            if (Format == EPixelFormat.PF_Unknown)
            {
                //?? check whether we can support this pixel format
#if DEBUG
                Log.Debug("Loading data for format {Format}", pixelFormatName);
#endif
                PlatformData = new FTexturePlatformData(Ar, this);

                if (Ar.Game == EGame.GAME_SeaOfThieves) Ar.Position += 4;

                if (Ar.AbsolutePosition != skipOffset)
                {
                    Log.Warning($"Texture2D read incorrectly. Offset {Ar.AbsolutePosition}, Skip Offset {skipOffset}, Bytes remaining {skipOffset - Ar.AbsolutePosition}");
                    Ar.SeekAbsolute(skipOffset, SeekOrigin.Begin);
                }

                Format = pixelFormat;
            }
            else
            {
#if DEBUG
                Log.Debug("Skipping data for format {Format}", pixelFormatName);
#endif
                Ar.SeekAbsolute(skipOffset, SeekOrigin.Begin);
            }

            pixelFormatName = Ar.ReadFName();
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("SizeX");
        writer.WriteValue(PlatformData.SizeX);

        writer.WritePropertyName("SizeY");
        writer.WriteValue(PlatformData.SizeY);

        writer.WritePropertyName("PackedData");
        writer.WriteValue(PlatformData.PackedData);

        writer.WritePropertyName("PixelFormat");
        writer.WriteValue(Format.ToString());

        if (PlatformData.OptData.ExtData != 0 && PlatformData.OptData.NumMipsInTail != 0)
        {
            writer.WritePropertyName("OptData");
            serializer.Serialize(writer, PlatformData.OptData);
        }

        writer.WritePropertyName("FirstMipToSerialize");
        writer.WriteValue(PlatformData.FirstMipToSerialize);

        if (PlatformData.Mips is { Length: > 0 })
        {
            writer.WritePropertyName("Mips");
            serializer.Serialize(writer, PlatformData.Mips);
        }

        if (PlatformData.VTData != null)
        {
            writer.WritePropertyName("VTData");
            serializer.Serialize(writer, PlatformData.VTData);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FTexture2DMipMap? GetFirstMip() => PlatformData.Mips.FirstOrDefault(x => x.BulkData.Data != null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FTexture2DMipMap? GetMipByMaxSize(int maxSize)
    {
        foreach (var mip in PlatformData.Mips)
        {
            if ((mip.SizeX <= maxSize || mip.SizeY <= maxSize) && mip.BulkData.Data != null)
                return mip;
        }

        return GetFirstMip();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FTexture2DMipMap? GetMipBySize(int sizeX, int sizeY)
    {
        foreach (var mip in PlatformData.Mips)
        {
            if (mip.SizeX == sizeX && mip.SizeY == sizeY && mip.BulkData.Data != null)
                return mip;
        }

        return GetFirstMip();
    }

    public override void GetParams(CMaterialParams parameters)
    {
        // Default empty method
        // ???
    }

    public override void GetParams(CMaterialParams2 parameters, EMaterialFormat format)
    {
        // Default empty method
        // ???
    }
}
