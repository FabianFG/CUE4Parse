using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;

namespace CUE4Parse_Conversion.Exporters;

public sealed class SkinnedAssetExporter(USkinnedAsset originalMesh) : MeshExporter<USkinnedAsset>(originalMesh)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(USkinnedAsset originalMesh, IMeshExportFormat format)
    {
        if (Session.Options.ExportMorphTargets)
        {
            originalMesh.PopulateMorphTargetVerticesData();
        }

        using var dto = new SkeletalMeshDto(originalMesh, Session.Options.MeshQuality, Session.Options.NaniteMeshFormat, Session.Options.ExportMorphTargets);
        if (dto.LODs.Count == 0)
        {
            throw new Exception("Skeletal mesh has no LODs");
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

        var materialPaths = EnqueueMaterials(dto.Materials);
        return format.BuildSkeletalMesh(ObjectName, ObjectPath, Session.Options, dto, materialPaths);
    }
}
