using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[JsonConverter(typeof(FSkeletalMaterialConverter))]
public class FSkeletalMaterial
{
    public ResolvedObject? Material; // UMaterialInterface
    public FName MaterialSlotName;
    public FName? ImportedMaterialSlotName;
    public FMeshUVChannelInfo? UVChannelData;
    public FPackageIndex OverlayMaterialInterface;

    public FSkeletalMaterial(FAssetArchive Ar)
    {
        Material = new FPackageIndex(Ar).ResolvedObject;
        if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.RefactorMeshEditorMaterials)
        {
            MaterialSlotName = Ar.ReadFName();
            var bSerializeImportedMaterialSlotName = !Ar.Owner.HasFlags(EPackageFlags.PKG_FilterEditorOnly);
            if (FCoreObjectVersion.Get(Ar) >= FCoreObjectVersion.Type.SkeletalMaterialEditorDataStripping)
            {
                bSerializeImportedMaterialSlotName = Ar.ReadBoolean();
            }

            if (bSerializeImportedMaterialSlotName)
            {
                ImportedMaterialSlotName = Ar.ReadFName();
            }
        }
        else
        {
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.MOVE_SKELETALMESH_SHADOWCASTING)
                Ar.Position += 4;

            if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RuntimeRecomputeTangent)
            {
                var bRecomputeTangent = Ar.ReadBoolean();
            }
        }
        if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
            UVChannelData = new FMeshUVChannelInfo(Ar);

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.MeshMaterialSlotOverlayMaterialAdded)
            OverlayMaterialInterface = new FPackageIndex(Ar);

        switch (Ar.Game)
        {
            case EGame.GAME_MarvelRivals:
                _ = new FGameplayTagContainer(Ar);
                break;
            case EGame.GAME_FragPunk or EGame.GAME_DaysGone or EGame.GAME_WorldofJadeDynasty:
                Ar.Position += 4;
                break;
            case EGame.GAME_Strinova:
                Ar.Position += 8;
                break;
        }
    }
}
