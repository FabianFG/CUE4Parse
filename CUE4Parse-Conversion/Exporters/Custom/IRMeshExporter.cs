using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Exporters.Custom;

public sealed class IRMeshExporter(UIRMesh originalMesh) : MeshExporter<UIRMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UIRMesh originalMesh, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(originalMesh, Session.Options.MeshQuality);
        if (dto.LODs.Count == 0)
            throw new Exception("IR mesh has no LODs");

        // UEFormat merges LODs into one file but we don't want that in this case
        // cause LODs can be separate meshes not real LODs
        if (Session.Options.MeshQuality == EMeshQuality.All && format is UEFormatMeshFormat)
            format = new UEFormatMeshFormat(bLodSeparate: true);

        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, EnqueueMaterials(dto.Materials));
    }
}
