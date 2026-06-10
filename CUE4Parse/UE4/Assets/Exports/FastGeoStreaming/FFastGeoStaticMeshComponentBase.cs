using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoStaticMeshComponentBase : FFastGeoMeshComponent
{
    public bool bUseDefaultCollision;

    public FFastGeoStaticMeshComponentBase(FFastGeoArchive Ar) : base(Ar)
    {
        SceneProxyDesc.StaticMeshSceneProxyDesc = new FStaticMeshSceneProxyDesc(Ar);
        bUseDefaultCollision = Ar.ReadBoolean();
        if (Ar.Game is EGame.GAME_WutheringWavesFastGeo) Ar.SkipFixedArray(sizeof(uint)*3);
    }
}

public class FFastGeoStaticMeshComponent : FFastGeoStaticMeshComponentBase
{
    public FFastGeoStaticMeshComponent(FFastGeoArchive Ar) : base(Ar) { }
}
