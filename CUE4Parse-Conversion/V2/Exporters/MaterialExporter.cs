using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Formats.Materials;
using CUE4Parse_Conversion.V2.Options;
using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class MaterialExporter(UMaterialInterface material) : ExporterBase(material)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles()
    {
        Log.Debug("Extracting material parameters (depth: {Depth})", Session.Options.MaterialDepth);

        var parameters = new CMaterialParams2();
        material.GetParams(parameters, Session.Options.MaterialDepth);

        var files = new List<ExportFile> { new JsonMaterialFormat().Build(ObjectName, parameters) };
        if (Session.Options.MeshFormat == EMeshFormat.USD)
        {
            files.Add(new UsdMaterialFormat().Build(ObjectName, parameters, PackageDirectory));
        }

        foreach (var texture in parameters.Textures.Values)
        {
            Session.Add(texture);
        }

        return files;
    }
}
