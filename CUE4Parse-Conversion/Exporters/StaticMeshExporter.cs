using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.Exporters;

public sealed class StaticMeshExporter(UStaticMesh originalMesh) : MeshExporter<UStaticMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UStaticMesh originalMesh, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(originalMesh, Session.Options.MeshQuality, Session.Options.NaniteMeshFormat);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Static mesh has no LODs");
        }

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, materialPaths);
    }
}

public sealed class GeometryCollectionExporter(UGeometryCollection originalMesh) : MeshExporter<UGeometryCollection>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UGeometryCollection originalMesh, IMeshExportFormat format)
    {
        using var dto = new StaticMeshDto(originalMesh, Session.Options.NaniteMeshFormat);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Geometry collection mesh has no LODs");
        }

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildStaticMesh(ObjectName, ObjectPath, Session.Options, dto, materialPaths);
    }
}
