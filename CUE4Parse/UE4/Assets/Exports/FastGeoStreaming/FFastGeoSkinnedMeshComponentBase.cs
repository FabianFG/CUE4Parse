namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoSkinnedMeshComponentBase : FFastGeoMeshComponent
{
    public ESkinCacheUsage[] SkinCacheUsage;
    public bool bOverrideMinLod;
    public bool bIncludeComponentLocationIntoBounds;
    public bool bHideSkin;
    public int MinLodModel;

    public FFastGeoSkinnedMeshComponentBase(FFastGeoArchive Ar) : base(Ar)
    {
        SkinCacheUsage = Ar.ReadArray<ESkinCacheUsage>();
        bOverrideMinLod = Ar.ReadBoolean();
        bIncludeComponentLocationIntoBounds = Ar.ReadBoolean();
        bHideSkin = Ar.ReadBoolean();
        MinLodModel = Ar.Read<int>();
        SceneProxyDesc.SkinnedMeshSceneProxyDesc = new FSkinnedMeshSceneProxyDesc(Ar);
    }
}

public class FFastGeoSkinnedMeshComponent : FFastGeoSkinnedMeshComponentBase
{
    public FFastGeoSkinnedMeshComponent(FFastGeoArchive Ar) : base(Ar) { }
}
