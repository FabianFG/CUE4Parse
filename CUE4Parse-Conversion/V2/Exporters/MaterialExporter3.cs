using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.V2.Formats.Materials;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class MaterialExporter3(UMaterialInterface material) : ExporterBase2(material)
{
    protected override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        Log.Debug("Extracting material parameters (format: {Format})", Session.Options.MaterialFormat);

        var parameters = new CMaterialParams2();
        material.GetParams(parameters, Session.Options.MaterialFormat);

        var file = new JsonMaterialFormat().Build(ObjectName, parameters);

        foreach (var ptr in parameters.Textures.Values)
        {
            if (ptr is UTexture texture)
            {
                Session.Add(new TextureExporter2(texture));
            }
        }

        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }
}
