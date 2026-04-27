using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Dto;

public readonly struct MeshMaterial(string? slotName, FPackageIndex? material = null)
{
    public readonly string SlotName = material?.Name ?? slotName ?? "None";
    public readonly FPackageIndex? Material = material;

    public MeshMaterial(FStaticMaterial material) : this(material.ImportedMaterialSlotName?.Text ?? material.MaterialSlotName.Text, material.MaterialInterface)
    {

    }

    public MeshMaterial(FSkeletalMaterial material) : this(material.ImportedMaterialSlotName?.Text ?? material.MaterialSlotName.Text, material.Material)
    {

    }
}
