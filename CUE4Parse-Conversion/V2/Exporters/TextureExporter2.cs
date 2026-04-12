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
    public override async Task<IReadOnlyList<ExportResult>> ExportAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        Log.Debug("Decoding texture for platform {Platform}", Session.Options.Platform);

        var decoded = texture.Decode(Session.Options.Platform);
        if (decoded == null)
        {
            return [ExportResult.Failure(ObjectName, PackagePath, PackageDirectory, new Exception("Failed to decode texture"))];
        }

        var format = GetTextureFormat(Session.Options.TextureFormat);
        var file = format.Build(decoded, Session.Options.ExportHdrTexturesAsHdr);
        var result = await WriteExportFileAsync(file, progress, ct).ConfigureAwait(false);
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
