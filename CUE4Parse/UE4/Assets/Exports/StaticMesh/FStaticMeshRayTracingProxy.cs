using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class FStaticMeshRayTracingProxy
{
    public FStaticMeshRayTracingProxyLOD[] LODs;
    
    public FStaticMeshRayTracingProxy(FAssetArchive Ar)
    {
        var stripFlags = new FStripDataFlags(Ar);

        var bUsingRenderingLODs = Ar.ReadBoolean();

        if (!stripFlags.IsAudioVisualDataStripped())
        {
            LODs = Ar.ReadArray(() => new FStaticMeshRayTracingProxyLOD(Ar));
        }
    }
}