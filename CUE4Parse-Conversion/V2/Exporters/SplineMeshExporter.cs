using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class SplineMeshExporter(USplineMeshComponent component) : MeshExporter<USplineMeshComponent>(component)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(USplineMeshComponent component, IMeshExportFormat format)
    {
        using var dto = new StaticMesh(component);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Spline mesh has no LODs");
        }

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, Session.Options, dto, materialPaths);
    }
}
