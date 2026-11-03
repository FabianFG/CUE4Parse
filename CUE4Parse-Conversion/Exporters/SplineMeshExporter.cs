using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Exporters;

public sealed class SplineMeshExporter(USplineMeshComponent component) : MeshExporter<USplineMeshComponent>(component)
{
    protected override IEnumerable<FPackageIndex?> MaterialReferences
        => component.GetStaticMesh().Load<UStaticMesh>()?.StaticMaterials.Select(slot => slot.MaterialInterface) ?? [];

    protected override IReadOnlyList<ExportFile> BuildFiles(USplineMeshComponent component, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(component, Session.Options.MeshQuality);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Spline mesh has no LODs");
        }

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, materialPaths);
    }
}
