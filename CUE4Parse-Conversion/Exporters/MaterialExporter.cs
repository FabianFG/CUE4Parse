using CUE4Parse_Conversion.Formats.Materials;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.Exporters;

public sealed class MaterialExporter(UMaterialInterface material) : ExporterBase(material)
{
    internal override IEnumerable<UObject> GetDependencies(CancellationToken ct) => LoadTextures(ReadParameters(), ct);

    private static IEnumerable<UTexture> LoadTextures(CMaterialParams2 parameters, CancellationToken ct)
    {
        foreach (var ptr in parameters.Textures.Values)
        {
            ct.ThrowIfCancellationRequested();
            if (ptr.TryLoad<UTexture>(out var texture)) yield return texture;
        }
    }

    private CMaterialParams2 ReadParameters()
    {
        var parameters = new CMaterialParams2();
        material.GetParams(parameters, Session.Options.MaterialDepth);
        return parameters;
    }

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Extracting material parameters (depth: {Depth})", Session.Options.MaterialDepth);

        var parameters = ReadParameters();

        var files = new List<ExportFile> { new JsonMaterialFormat().Build(ObjectName, parameters) };
        if (Session.Options.MeshFormat == EMeshFormat.USD)
        {
            files.Add(new UsdMaterialFormat().Build(ObjectName, parameters, SaveDirectory));
        }

        foreach (var texture in LoadTextures(parameters, ct))
        {
            Session.Add(texture);
        }

        return files;
    }
}
