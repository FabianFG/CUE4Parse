using CUE4Parse_Conversion.Formats.Materials;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.Exporters;

public sealed class MaterialExporter(UMaterialInterface material) : ExporterBase(material)
{
    private CMaterialParams2? _parameters;
    private ExportSessionCache? _parameterCache;
    internal override IEnumerable<UObject> GetDependencies(CancellationToken ct)
    {
        foreach (var texture in LoadTextures(ReadParameters(), ct))
            yield return texture;

        if (Session.ResolveMaterial(material).OpacityTexture is { } opacity)
            yield return opacity;
    }

    private static IEnumerable<UTexture> LoadTextures(CMaterialParams2 parameters, CancellationToken ct)
    {
        foreach (var ptr in parameters.Textures.Values.Distinct())
        {
            ct.ThrowIfCancellationRequested();
            if (ptr.TryLoad<UTexture>(out var texture)) yield return texture;
        }
    }

    private CMaterialParams2 ReadParameters()
    {
        if (!ReferenceEquals(_parameterCache, Session.Cache))
        {
            _parameters = null;
            _parameterCache = Session.Cache;
        }

        return _parameters ??= Session.Cache.ReadParameters(material, Session.Options.MaterialDepth);
    }

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Extracting material parameters (depth: {Depth})", Session.Options.MaterialDepth);

        var parameters = ReadParameters();

        var files = new List<ExportFile> { new JsonMaterialFormat().Build(ObjectName, parameters) };
        if (Session.Options.MeshFormat is EMeshFormat.USD)
        {
            files.Add(new UsdMaterialFormat(Session).Build(ObjectName, Session.ResolveMaterial(material), SaveDirectory));
        }

        if (!Session.AttachMaterials)
        {
            foreach (var texture in LoadTextures(parameters, ct))
            {
                Session.Add(texture);
            }
        }

        return files;
    }
}
