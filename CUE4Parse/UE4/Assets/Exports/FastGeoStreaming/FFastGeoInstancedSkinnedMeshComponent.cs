using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public struct FSkinnedMeshInstanceData(FFastGeoArchive Ar)
{
    public FTransform Transform = Ar.Read<FTransform>();
    public uint AnimationIndex = Ar.Read<uint>();
}

public class FFastGeoInstancedSkinnedMeshComponent : FFastGeoSkinnedMeshComponentBase
{
    public FSkinnedMeshInstanceData[] InstanceData;
    public int NumCustomDataFloats;
    public float[] InstanceCustomData;

    public FFastGeoInstancedSkinnedMeshComponent(FFastGeoArchive Ar) : base(Ar)
    {
        InstanceData = Ar.ReadArray(() => new FSkinnedMeshInstanceData(Ar));
        NumCustomDataFloats = Ar.Read<int>();
        InstanceCustomData = Ar.ReadArray<float>();
        SceneProxyDesc.InstancedSkinnedMeshSceneProxyDesc = new FInstancedSkinnedMeshSceneProxyDesc(Ar);
    }
}
