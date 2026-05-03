using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Meshes;
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

        var files = new List<ExportFile> { new JsonMaterialFormat().Build(ObjectName, parameters) };
        if (Session.Options.MeshFormat == EMeshFormat.USD)
        {
            files.Add(new UsdMaterialFormat().Build(ObjectName, parameters, PackageDirectory));
        }

        foreach (var ptr in parameters.Textures.Values)
        {
            if (ptr is UTexture texture)
            {
                Session.Add(new TextureExporter2(texture));
            }
        }

        var tasks = files.Select(file => WriteExportFileAsync(file, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }
}
