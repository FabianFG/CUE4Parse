using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoProceduralISMComponent : FFastGeoStaticMeshComponentBase
{
    public int NumInstances;
    public int NumCustomDataFloats;
    public FBox PrimitiveBoundsOverride;

    public FFastGeoProceduralISMComponent(FFastGeoArchive Ar) : base(Ar)
    {
        NumInstances = Ar.Read<int>();
        NumCustomDataFloats = Ar.Read<int>();
        PrimitiveBoundsOverride = new FBox(Ar);
        SceneProxyDesc.InstancedStaticMeshSceneProxyDesc = new FInstancedStaticMeshSceneProxyDesc(Ar);
    }
}
