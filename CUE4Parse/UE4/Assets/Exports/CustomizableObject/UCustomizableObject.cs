using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UCustomizableObject : UObject
{
    // private const int CurrentVersion = 414;

    public FMorphTargetInfo[] ContributingMorphTargetsInfo;
    public FMorphTargetVertexData[] MorphTargetReconstructionData;
    public FCustomizableObjectMeshToMeshVertData[] ClothMeshToMeshVertData;
    public FCustomizableObjectClothingAssetData[] ContributingClothingAssetsData;
    public FCustomizableObjectClothConfigData[] ClothSharedConfigsData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var version = Ar.Read<int>();

        // if (CurrentVersion == version)
        {
            ContributingMorphTargetsInfo = Ar.ReadArray<FMorphTargetInfo>();
            MorphTargetReconstructionData = Ar.ReadArray<FMorphTargetVertexData>();
        }

        {
            ClothMeshToMeshVertData = Ar.ReadBulkArray(() => new FCustomizableObjectMeshToMeshVertData(Ar));
            ContributingClothingAssetsData = Ar.ReadArray(() => new FCustomizableObjectClothingAssetData(Ar));
            ClothSharedConfigsData = Ar.ReadArray(() => new FCustomizableObjectClothConfigData(Ar));
        }

        var model = new Model(Ar);
    }
}