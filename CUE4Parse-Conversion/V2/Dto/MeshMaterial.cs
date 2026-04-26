using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Dto;

public class MeshMaterial(string? slotName, FPackageIndex? material = null)
{
    public readonly string SlotName = slotName ?? material?.Name ?? "None";
    public readonly FPackageIndex? Material = material;

    public MeshMaterial(FStaticMaterial material) : this(material.MaterialSlotName.Text, material.MaterialInterface)
    {

    }

    public MeshMaterial(FSkeletalMaterial material) : this(material.MaterialSlotName.Text, material.Material)
    {

    }
}
