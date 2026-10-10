using CUE4Parse.UE4.Assets.Exports.Houdini;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Exporters.Custom;

public sealed class HoudiniAssetExporter(UHoudiniAsset asset) : ExporterBase(asset)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        if (asset.HdaBuffer.Length == 0)
            throw new Exception("Houdini asset contains no data");

        return [new ExportFile("hda", asset.HdaBuffer)];
    }
}

public sealed class HoudiniStaticMeshExporter(UHoudiniStaticMesh mesh) : MeshExporter<UHoudiniStaticMesh>(mesh)
{
    protected override IEnumerable<FPackageIndex?> MaterialReferences => mesh.Materials;

    protected override IReadOnlyList<ExportFile> BuildFiles(UHoudiniStaticMesh original, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(original);
        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, materialPaths);
    }
}
