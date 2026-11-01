using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.Exporters;

public sealed class TextureExporter(UTexture texture) : ExporterBase(texture)
{
    private string? _texturePath;

    internal void CacheTexturePath()
    {
        if (_texturePath != null)
        {
            Session.Cache.CacheTexturePath(texture, _texturePath);
        }
    }

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Decoding texture for platform {Platform} as {Format}", Session.Options.TexturePlatform, Session.Options.TextureFormat);

        var files = new List<ExportFile>();
        _texturePath = null;
        var firstMip = texture.GetFirstMipIndex();
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } firstVt } && firstVt.IsInitialized())
        {
            firstMip = Math.Max(firstMip, TextureDecoder.GetMinLevel(firstVt));
        }

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
            AddMip(firstMip);
        }

        return files;

        void AddMip(int index)
        {
            ct.ThrowIfCancellationRequested();

            var platform = Session.Options.TexturePlatform;
            CTexture?[]? decoded = texture switch
            {
                UTexture2DArray array => array.DecodeTextureArray(index, platform),
                UTextureCube => texture.DecodeMip(index, platform)?.ToPanorama() is { } panorama ? [panorama] : null,
                _ => texture.DecodeMip(index, platform) is { } single ? [single] : null
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

                var data = slice.Encode(Session.Options, out var extension);
                var file = new ExportFile(extension, data, layered ? $"{mipSuffix}_LAYER{i}" : mipSuffix);
                files.Add(file);

                if (Session.AttachMaterials && index == firstMip && i == 0 && texture is not UTextureCube)
                {
                    (_, _texturePath) = ResolveOutputPath(file);
                }
            }
        }
    }
}
