using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.Exporters;

public sealed class TextureExporter(UTexture texture) : ExporterBase(texture)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Decoding texture for platform {Platform} as {Format}", Session.Options.TexturePlatform, Session.Options.TextureFormat);

        if (Session.Options.ExportAllTextureMips)
        {
            var files = new List<ExportFile>();
            if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
            {
                for (var mipIndex = TextureDecoder.GetMinLevel(vt); mipIndex < vt.NumMips; mipIndex++)
                {
                    AddMip(files, mipIndex, ct);
                }
            }
            else
            {
                for (var mipIndex = 0; mipIndex < texture.PlatformData.Mips.Length; mipIndex++)
                {
                    AddMip(files, mipIndex, ct);
                }
            }

            return files;
        }

        var decoded = texture.Decode(Session.Options.TexturePlatform)
            ?? throw new Exception("Failed to decode texture");

        if (texture is UTextureCube)
            decoded = decoded.ToPanorama();

        var data = decoded.Encode(Session.Options, out var ext);
        return [new ExportFile(ext, data)];
    }

    private void AddMip(List<ExportFile> files, int mipIndex, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var decoded = texture.DecodeMip(mipIndex, Session.Options.TexturePlatform);
        if (decoded == null)
            return;

        if (texture is UTextureCube)
            decoded = decoded.ToPanorama();

        var data = decoded.Encode(Session.Options, out var ext);
        files.Add(new ExportFile(ext, data, $"_{mipIndex}"));
    }
}
