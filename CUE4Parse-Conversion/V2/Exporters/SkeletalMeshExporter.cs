using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse_Conversion.V2.Formats.Meshes;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class SkeletalMeshExporter(USkeletalMesh originalMesh) : MeshExporter2<USkeletalMesh>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(USkeletalMesh originalMesh, IMeshExportFormat format)
    {
        if (Session.Options.ExportMorphTargets)
        {
            originalMesh.PopulateMorphTargetVerticesData();
        }

        var dto = new SkeletalMesh(originalMesh);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Skeletal mesh has no LODs");
        }

        if (Session.Options.ExportMaterials)
        {
            EnqueueMaterials(dto.Materials);
        }

        if (dto.AssetUserData != null)
        {
            foreach (var userData in dto.AssetUserData)
            {
                if (userData.TryLoad<UDNAAsset>(out var dna))
                {
                    Session.Add(new DnaExporter(dna));
                }
            }
        }

        return format.BuildSkeletalMesh(ObjectName, Session.Options, dto);
    }
}
