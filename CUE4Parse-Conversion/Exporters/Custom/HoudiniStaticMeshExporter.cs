using CUE4Parse.UE4.Assets.Exports.Houdini;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;

namespace CUE4Parse_Conversion.Exporters.Custom;

public sealed class HoudiniStaticMeshExporter(UHoudiniStaticMesh mesh) : MeshExporter<UHoudiniStaticMesh>(mesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UHoudiniStaticMesh original, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(original);
        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, materialPaths);
    }
}
