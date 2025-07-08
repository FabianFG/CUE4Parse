using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UModelComponent : UPrimitiveComponent
{
    public FPackageIndex Model;
    /** The elements used to render the nodes. */
    public FModelElement[] Elements = [];
    /** The index of this component in the ULevel's ModelComponents array. */
    public int ComponentIndex;
    /** The nodes which this component renders. */
    public ushort[] Nodes;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Model = new FPackageIndex(Ar);
        if (Ar.Ver <= EUnrealEngineObjectUE4Version.REMOVE_ZONES_FROM_MODEL)
            Ar.Position += 4; // DummyZoneIndex
        Elements = Ar.ReadArray(() => new FModelElement(Ar));
        ComponentIndex = Ar.Read<int>();
        Nodes = Ar.ReadArray<ushort>();
    }
}

public class FModelElement
{
    /** The model component containing this element. */
    public FPackageIndex Component;
    /** The material used by the nodes in this element. */
    public FPackageIndex Material;
    public FMeshMapBuildData? LegacyMapBuildData;
    /** The nodes in the element. */
    public ushort[] Nodes;
    /** Uniquely identifies this component's built map data. */
    public FGuid? MapBuildDataId;

    public FModelElement(FAssetArchive Ar)
    {
        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
        {
            LegacyMapBuildData = new FMeshMapBuildData();
            LegacyMapBuildData.LightMap = Ar.Read<ELightMapType>() switch
            {
                ELightMapType.LMT_1D => new FLegacyLightMap1D(Ar),
                ELightMapType.LMT_2D => new FLightMap2D(Ar),
                _ => null
            };
            LegacyMapBuildData.ShadowMap = Ar.Read<EShadowMapType>() switch
            {
                EShadowMapType.SMT_2D => new FShadowMap2D(Ar),
                _ => null
            };
        }

        if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.FixedBSPLightmaps)
        {
            MapBuildDataId = Ar.Read<FGuid>();
        }

        Component = new FPackageIndex(Ar);
        Material = new FPackageIndex(Ar);
        Nodes = Ar.ReadArray<ushort>();

        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
        {
            LegacyMapBuildData.IrrelevantLights = Ar.ReadArray<FGuid>();
        }
    }
}
