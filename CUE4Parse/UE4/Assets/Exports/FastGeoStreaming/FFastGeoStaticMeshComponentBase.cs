namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoStaticMeshComponentBase : FFastGeoMeshComponent
{
    public bool bUseDefaultCollision;

    public FFastGeoStaticMeshComponentBase(FFastGeoArchive Ar) : base(Ar)
    {
        SceneProxyDesc.StaticMeshSceneProxyDesc = new FStaticMeshSceneProxyDesc(Ar);
        bUseDefaultCollision = Ar.ReadBoolean();
    }
}

public class FFastGeoStaticMeshComponent : FFastGeoStaticMeshComponentBase
{
    public FFastGeoStaticMeshComponent(FFastGeoArchive Ar) : base(Ar) { }
}
