using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.Exporters;

public sealed class TextureExporter(UTexture texture) : ExporterBase(texture)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Decoding texture for platform {Platform} as {Format}", Session.Options.TexturePlatform, Session.Options.TextureFormat);

        var files = new List<ExportFile>();
        var all = Session.Options.ExportAllTextureMips;
        if (all)
        {
            if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
            {
                for (var i = TextureDecoder.GetMinLevel(vt); i < vt.NumMips; i++)
                {
                    AddMip(i);
                }
            }
            else for (var i = 0; i < texture.PlatformData.Mips.Length; i++)
            {
                if (texture.PlatformData.Mips[i].EnsureValidBulkData(texture.MipDataProvider, i))
                {
                    AddMip(i);
                }
                else
                {
                    Log.Warning("Texture mip {Index} has no valid bulk data, skipping", i);
                }
            }
        }
        else
        {
            AddMip(texture.GetFirstMipIndex());
        }

        return files;

        void AddMip(int index)
        {
            ct.ThrowIfCancellationRequested();

            var platform = Session.Options.TexturePlatform;
            CTexture?[]? decoded = texture switch
            {
                UTexture2DArray array => array.DecodeTextureArray(index, platform),
                UTextureCube => DecodeMip(index)?.ToPanorama() is { } panorama ? [panorama] : null,
                _ => DecodeMip(index) is { } single ? [single] : null
            };

            if (decoded is not { Length: > 0 })
            {
                if (!all)
                {
                    throw new Exception($"Failed to decode texture mip {index}");
                }

                Log.Warning("Failed to decode texture mip {Index}, skipping", index);
                return;
            }

            var mipSuffix = all ? $"_MIP{index}" : null;
            var layered = texture is UTexture2DArray;
            for (var i = 0; i < decoded.Length; i++)
            {
                if (decoded[i] is not { } slice)
                    continue;

                files.Add(Encode(slice) with { NameSuffix = layered ? $"{mipSuffix}_LAYER{i}" : mipSuffix });
            }
        }
    }

    private CTexture? DecodeMip(int index) => Session.TextureCache is { } cache ? cache.Decode(texture, index) : texture.DecodeMip(index, Session.Options.TexturePlatform);

    private ExportFile Encode(CTexture decoded)
    {
        if (Session.TextureCache is { } cache)
            return cache.Encode(decoded, Session.Options);

        var data = decoded.Encode(Session.Options, out var extension);
        return new ExportFile(extension, data);
    }
}
