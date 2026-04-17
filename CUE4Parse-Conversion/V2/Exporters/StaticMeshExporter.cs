using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class StaticMeshExporter(UStaticMesh originalMesh) : MeshExporter2<UStaticMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UStaticMesh originalMesh, IMeshExportFormat format)
    {
        if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
        {
            throw new Exception("Failed to convert static mesh or no LODs");
        }

        if (Session.Options.ExportMaterials)
        {
            EnqueueMaterials(originalMesh.Materials);
        }

        return format.BuildStaticMesh(ObjectName, Session.Options, convertedMesh);
    }
}
