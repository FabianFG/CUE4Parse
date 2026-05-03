using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class TextureExporter(UTexture texture) : ExporterBase(texture)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles()
    {
        Log.Debug("Decoding texture for platform {Platform} as {Format}", Session.Options.TexturePlatform, Session.Options.TextureFormat);

        var decoded = texture.Decode(Session.Options.TexturePlatform);
        if (decoded == null)
        {
            throw new Exception("Failed to decode texture");
        }

        if (texture is UTextureCube)
        {
            decoded = decoded.ToPanorama();
        }

        var data = decoded.Encode(Session.Options, out var ext);
        return [new ExportFile(ext, data)];
    }
}
