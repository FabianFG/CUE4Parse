using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Chaos;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.ChaosClothAsset;

public class UChaosClothAsset : USkinnedAsset
{
    /** Cloth Collection containing this asset data. One per LOD. */
    public FManagedArrayCollection[] ClothCollections;
    /** Simulation mesh Lods as fed to the solver for constraints creation. Ownership gets transferred to the proxy when it is changed during a simulation. */
    public FStructFallback? ClothSimulationModel;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bHasVertexColors = Ar.Game >= GAME_UE5_6 ? GetOrDefault<bool>("bHasVertexColors") : false;
        LODInfo = GetOrDefault<FSkeletalMeshLODGroupSettings[]?>(nameof(LODInfo));
        Skeleton = GetOrDefault(nameof(Skeleton), new FPackageIndex());
        SkeletalMaterials = GetOrDefault<FSkeletalMaterial[]>(nameof(Materials), []);
        PhysicsAsset = GetOrDefault(nameof(PhysicsAsset), new FPackageIndex());
        Bounds = GetOrDefault<FBoxSphereBounds?>(nameof(Bounds), null);
        AssetUserData = GetOrDefault(nameof(AssetUserData), Array.Empty<FPackageIndex>());

#if DEBUG
        Log.Debug(nameof(UChaosClothAsset));
#endif

        if (Ar.Game is GAME_TheBloodofDawnwalker) bHasVertexColors = true;

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.AddClothAssetBase)
        {
            ReferenceSkeleton = new FReferenceSkeleton(Ar);
        }

        var bCooked = Ar.ReadBoolean();
        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.ClothCollectionSingleLodSchema)
        {
            // Cloth assets before this version had a single ClothCollection with a completely different schema.
            ClothCollections = Ar.ReadArray(() => new FManagedArrayCollection(new FChaosArchive(Ar)));
        }
        else
        {
            ClothCollections = Ar.ReadArray(() => new FManagedArrayCollection(new FChaosArchive(Ar)));
        }

        if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.AddClothAssetBase)
        {
            ReferenceSkeleton = new FReferenceSkeleton(Ar);
        }

        // this should be FSkeletalMeshRenderData::Serialize, like in USkeletalMesh
        if (bCooked)
        {
            // in UE5.0+ this check is also inside Ar.FilterEditorOnly block
            if (Ar.Versions["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"])
            {
                var minMobileLODIdx = Ar.Read<int>();
            }
            
            LODModels = new FStaticLODModel[Ar.Read<int>()];
            for (var i = 0; i < LODModels.Length; i++)
            {
                LODModels[i] = new FStaticLODModel();
                LODModels[i].SerializeRenderItem(Ar, bHasVertexColors);
            }

            if (Ar.Game >= GAME_UE5_5)
            {
                NaniteResources = new FNaniteResources(Ar);
            }

            var numInlinedLODs = Ar.Read<byte>();
            var numNonOptionalLODs = Ar.Read<byte>();

            if (Ar.Game >= GAME_UE5_4)
            {
                ClothSimulationModel = new FStructFallback(Ar, "ChaosClothSimulationModel");
            }
        }

        Materials = new FPackageIndex?[SkeletalMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = SkeletalMaterials[i]?.MaterialInterface;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(ClothCollections));
        serializer.Serialize(writer, ClothCollections);

        writer.WritePropertyName(nameof(ReferenceSkeleton));
        serializer.Serialize(writer, ReferenceSkeleton);

        writer.WritePropertyName(nameof(LODModels));
        serializer.Serialize(writer, LODModels);

        writer.WritePropertyName(nameof(NaniteResources));
        serializer.Serialize(writer, NaniteResources);

        writer.WritePropertyName(nameof(ClothSimulationModel));
        serializer.Serialize(writer, ClothSimulationModel);
    }
}
