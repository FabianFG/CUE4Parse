using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.ChaosClothAsset;

public class FChaosClothSimulationLodModel : FStructFallback
{
    /** Weight maps for storing painted attributes modifiers on constraint properties. */
    public Dictionary<FName, float[]> WeightMaps;
    /** LOD Transition mesh to mesh skinning weights. */
    public FMeshToMeshVertData[] LODTransitionUpData;
    public FMeshToMeshVertData[] LODTransitionDownData;
    /** Vertex sets */
    public Dictionary<FName, int[]> VertexSets;
    /** Face int maps (currently used by cloth collision layers)*/
    public Dictionary<FName, int[]> FaceIntMaps;
    /** Face sets */
    public Dictionary<FName, int[]> FaceSets;

    public FChaosClothSimulationLodModel(FAssetArchive Ar) : base(Ar, "ChaosClothSimulationLodModel")
    {
        var bCooked = Ar.ReadBoolean();
        WeightMaps = Ar.ReadMap(Ar.ReadFName, Ar.ReadArray<float>);
        LODTransitionUpData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
        LODTransitionDownData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
        VertexSets = Ar.ReadMap(Ar.ReadFName, Ar.ReadArray<int>);
        FaceIntMaps = Ar.ReadMap(Ar.ReadFName, Ar.ReadArray<int>);
        FaceSets = Ar.ReadMap(Ar.ReadFName, Ar.ReadArray<int>);
    }
}
