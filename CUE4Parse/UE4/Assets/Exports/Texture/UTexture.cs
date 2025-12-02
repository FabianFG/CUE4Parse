using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UBinkMediaTexture : UTexture;

public abstract class UTexture : UUnrealMaterial, IAssetUserData
{
    public FGuid LightingGuid { get; private set; }
    public TextureCompressionSettings CompressionSettings { get; private set; }
    public TextureGroup LODGroup { get; private set; }
    public TextureFilter Filter { get; private set; }
    public bool SRGB { get; private set; }
    public FPackageIndex[] AssetUserData { get; private set; } = [];
    public EPixelFormat Format { get; protected set; } = EPixelFormat.PF_Unknown;
    public FTexturePlatformData PlatformData { get; private set; } = new();
    public FEditorBulkData? EditorData { get; private set; }

    public bool RenderNearestNeighbor => LODGroup == TextureGroup.TEXTUREGROUP_Pixels2D || Filter == TextureFilter.TF_Nearest;
    public bool IsNormalMap => CompressionSettings == TextureCompressionSettings.TC_Normalmap;
    public bool IsHDR => CompressionSettings is
        TextureCompressionSettings.TC_HDR or
        TextureCompressionSettings.TC_HDR_F32 or
        TextureCompressionSettings.TC_HDR_Compressed or
        TextureCompressionSettings.TC_HalfFloat or
        TextureCompressionSettings.TC_SingleFloat;

    public virtual TextureAddress GetTextureAddressX() => TextureAddress.TA_Wrap;
    public virtual TextureAddress GetTextureAddressY() => TextureAddress.TA_Wrap;
    public virtual TextureAddress GetTextureAddressZ() => TextureAddress.TA_Wrap;

    private UTextureAllMipDataProviderFactory? _mipDataProvider;
    public UTextureAllMipDataProviderFactory? MipDataProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_mipDataProvider is null)
            {
                foreach (var aud in AssetUserData)
                {
                    if (aud.TryLoad<UTextureAllMipDataProviderFactory>(out _mipDataProvider))
                    {
                        break;
                    }
                }
            }
            return _mipDataProvider;
        }
    }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 16;
        base.Deserialize(Ar, validPos);
        LightingGuid = GetOrDefault(nameof(LightingGuid), new FGuid((uint) GetFullName().GetHashCode()));
        CompressionSettings = GetOrDefault(nameof(CompressionSettings), TextureCompressionSettings.TC_Default);
        LODGroup = GetOrDefault(nameof(LODGroup), TextureGroup.TEXTUREGROUP_World);
        Filter = GetOrDefault(nameof(Filter), TextureFilter.TF_Nearest);
        SRGB = GetOrDefault(nameof(SRGB), true);
        AssetUserData = GetOrDefault<FPackageIndex[]>(nameof(AssetUserData), []);

        var stripFlags = new FStripDataFlags(Ar);

        // If archive is has editor only data
        if (!stripFlags.IsEditorDataStripped())
        {
            if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.VirtualizedBulkDataHaveUniqueGuids)
            {
                if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.TextureSourceVirtualization)
                {
                    new FByteBulkData(Ar);
                }
                else
                {
                    EditorData = new FEditorBulkData(Ar);
                }
            }
            else
            {
                EditorData = new FEditorBulkData(Ar);
            }
        }
    }

    protected void DeserializeCookedPlatformData(FAssetArchive Ar, bool bSerializeMipData = true)
    {
        var pixelFormatName = Ar.ReadFName();
        if (pixelFormatName.Text == "PF_BC6H_Signed") pixelFormatName = "PF_BC6H";
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
                PlatformData = new FTexturePlatformData(Ar, this, bSerializeMipData);

                if (Ar.Game is EGame.GAME_SeaOfThieves or EGame.GAME_DeltaForceHawkOps) Ar.Position += 4;

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
    public FTexture2DMipMap? GetMip(int index) =>
        index >= 0 && index < PlatformData.Mips.Length && PlatformData.Mips[index].EnsureValidBulkData(MipDataProvider, index)
            ? PlatformData.Mips[index]
            : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FTexture2DMipMap? GetFirstMip() => PlatformData.Mips.Where((t, i) => t.EnsureValidBulkData(MipDataProvider, i)).FirstOrDefault();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FTexture2DMipMap? GetMipByMaxSize(int maxSize)
    {
        for (var i = 0; i < PlatformData.Mips.Length; i++)
        {
            var mip = PlatformData.Mips[i];
            if ((mip.SizeX <= maxSize || mip.SizeY <= maxSize) && mip.EnsureValidBulkData(MipDataProvider, i))
                return mip;
        }

        return GetFirstMip();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FTexture2DMipMap? GetMipBySize(int sizeX, int sizeY)
    {
        for (var i = 0; i < PlatformData.Mips.Length; i++)
        {
            var mip = PlatformData.Mips[i];
            if (mip.SizeX == sizeX && mip.SizeY == sizeY && mip.EnsureValidBulkData(MipDataProvider, i))
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
