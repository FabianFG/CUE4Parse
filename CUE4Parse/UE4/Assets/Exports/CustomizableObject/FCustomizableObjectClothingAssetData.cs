using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FCustomizableObjectClothingAssetData
{
    public FClothLODDataCommon[] LodData;
    public int[] LodMap;
    public FName[] UsedBoneNames;
    public int[] UsedBoneIndices;
    public int ReferenceBoneIndex;
    public FCustomizableObjectClothConfigData[] ConfigsData;
    public string PhysicsAssetPath;
    public FName Name;
    public FGuid OriginalAssetGuid;
    
    public FCustomizableObjectClothingAssetData(FAssetArchive Ar)
    {
        LodData = Ar.ReadArray(() => new FClothLODDataCommon(Ar));
        LodMap = Ar.ReadArray<int>();
        UsedBoneNames = Ar.ReadArray(Ar.ReadFName);
        UsedBoneIndices = Ar.ReadArray<int>();
        ReferenceBoneIndex = Ar.Read<int>();
        ConfigsData = Ar.ReadArray(() => new FCustomizableObjectClothConfigData(Ar));
        PhysicsAssetPath = Ar.ReadFString();
        Name = Ar.ReadFName();
        OriginalAssetGuid = Ar.Read<FGuid>();
    }
}