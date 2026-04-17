using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class SplineMeshExporter(USplineMeshComponent component) : MeshExporter2<USplineMeshComponent>(component)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(USplineMeshComponent component, IMeshExportFormat format)
    {
        var originalMesh = component.GetStaticMesh().Load<UStaticMesh>();
        if (originalMesh == null)
        {
            throw new Exception("Failed to load static mesh");
        }

        if (!originalMesh.TryConvert(component, out var convertedMesh) || convertedMesh.LODs.Count == 0)
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
