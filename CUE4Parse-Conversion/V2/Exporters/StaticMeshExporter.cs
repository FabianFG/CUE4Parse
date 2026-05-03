using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class StaticMeshExporter(UStaticMesh originalMesh) : MeshExporter2<UStaticMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UStaticMesh originalMesh, IMeshExportFormat format)
    {
        using var dto = new StaticMesh(originalMesh);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Static mesh has no LODs");
        }

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, Session.Options, dto, materialPaths);
    }
}
