using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;

namespace CUE4Parse_Conversion.Exporters.Custom;

public sealed class IRMeshExporter(UIRMesh originalMesh) : MeshExporter<UIRMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UIRMesh originalMesh, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(originalMesh);
        if (dto.LODs.Count == 0)
            throw new Exception("IR mesh has no LODs");

        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, EnqueueMaterials(dto.Materials));
    }
}
