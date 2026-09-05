using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[StructFallback]
[JsonConverter(typeof(FSkeletalMaterialConverter))]
public class FSkeletalMaterial
{
    public FPackageIndex? MaterialInterface; // UMaterialInterface
    public FName MaterialSlotName;
    public FName? ImportedMaterialSlotName;
    public FMeshUVChannelInfo? UVChannelData;
    public FPackageIndex? OverlayMaterialInterface;

    public FSkeletalMaterial(FPackageIndex materialInterface)
    {
        MaterialInterface = materialInterface;
    }

    public FSkeletalMaterial(FAssetArchive Ar)
    {
        MaterialInterface = new FPackageIndex(Ar);
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
            case GAME_MarvelRivals:
                _ = new FGameplayTagContainer(Ar);
                break;
            case GAME_FragPunk or GAME_DaysGone or GAME_WorldofJadeDynasty or GAME_AssaultFireFuture:
                Ar.Position += 4;
                break;
            case GAME_Strinova:
                Ar.Position += 8;
                break;
        }
    }

    public FSkeletalMaterial(FStructFallback fallback)
    {
        MaterialInterface = fallback.GetOrDefault(nameof(MaterialInterface), new FPackageIndex());
        MaterialSlotName = fallback.GetOrDefault<FName>(nameof(MaterialSlotName), "None");
        UVChannelData = fallback.GetOrDefault<FMeshUVChannelInfo>(nameof(UVChannelData), null);
    }
}
