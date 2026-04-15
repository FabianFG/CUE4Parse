using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class SkeletalMeshExporter(USkeletalMesh originalMesh) : MeshExporter2<USkeletalMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(USkeletalMesh originalMesh, IMeshExportFormat format)
    {
        if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
        {
            throw new Exception("Failed to convert skeletal mesh or no LODs");
        }

        if (Session.Options.ExportMaterials)
        {
            foreach (var ptr in originalMesh.Materials)
            {
                if (ptr?.TryLoad<UMaterialInterface>(out var material) == true)
                {
                    Session.Add(new MaterialExporter3(material));
                }
            }
        }

        foreach (var userData in originalMesh.AssetUserData ?? [])
        {
            if (userData.TryLoad<UDNAAsset>(out var dna))
            {
                Session.Add(new DnaExporter(dna));
            }
        }

        return format.BuildSkeletalMesh(ObjectName, Session.Options, originalMesh, convertedMesh);
    }
}
