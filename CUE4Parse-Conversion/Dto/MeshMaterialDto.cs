using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public readonly struct MeshMaterialDto(string? slotName, FPackageIndex? material = null)
{
    public readonly string SlotName = slotName ?? "DefaultSlot";
    public readonly FPackageIndex? Material = material;
    public string Name => Material is { IsNull: false } ? Material.Name : SlotName;

    public MeshMaterialDto(FStaticMaterial material) : this(material.ImportedMaterialSlotName?.Text ?? material.MaterialSlotName?.Text, material.MaterialInterface)
    {

    }

    public MeshMaterialDto(FSkeletalMaterial material) : this(material.ImportedMaterialSlotName?.Text ?? material.MaterialSlotName?.Text, material.MaterialInterface)
    {

    }
}
