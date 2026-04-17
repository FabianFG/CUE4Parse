using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.V2.Formats.Textures;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class TextureExporter2(UTexture texture) : ExporterBase2(texture)
{
    protected override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        Log.Debug("Decoding texture for platform {Platform}", Session.Options.Platform);

        var decoded = texture.Decode(Session.Options.Platform);
        if (decoded == null)
        {
            throw new Exception("Failed to decode texture");
        }

        if (texture is UTextureCube)
        {
            decoded = decoded.ToPanorama();
        }

        var format = GetTextureFormat(Session.Options.TextureFormat);
        var file = format.Build(decoded, Session.Options.ExportHdrTexturesAsHdr);
        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }

    private ITextureExportFormat GetTextureFormat(ETextureFormat format) => format switch
    {
        ETextureFormat.Png => new PngTextureFormat(),
        ETextureFormat.Jpeg => new JpegTextureFormat(),
        ETextureFormat.Tga => new TgaTextureFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
    };
}
