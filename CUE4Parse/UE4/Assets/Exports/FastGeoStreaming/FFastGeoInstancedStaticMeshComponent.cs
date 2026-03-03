using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

[StructLayout(LayoutKind.Sequential)]
public struct FInstancedStaticMeshRandomSeed
{
    public int StartInstanceIndex;
    public int RandomSeed;
}

public class FFastGeoInstancedStaticMeshComponent : FFastGeoStaticMeshComponentBase
{
    public FInstancedStaticMeshInstanceData[] PerInstanceSMData;
    public int InstancingRandomSeed;
    public float[] PerInstanceSMCustomData;
    public FInstancedStaticMeshRandomSeed[] AdditionalRandomSeeds;
    public FBox NavigationBounds;

    public FFastGeoInstancedStaticMeshComponent(FFastGeoArchive Ar) : base(Ar)
    {
        PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar));
        InstancingRandomSeed = Ar.Read<int>();
        PerInstanceSMCustomData = Ar.ReadBulkArray(Ar.Read<float>);
        AdditionalRandomSeeds = Ar.ReadArray<FInstancedStaticMeshRandomSeed>();
        NavigationBounds = new FBox(Ar);
        SceneProxyDesc.InstancedStaticMeshSceneProxyDesc = new FInstancedStaticMeshSceneProxyDesc(Ar);
    }
}
