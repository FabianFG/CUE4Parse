using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Decal;

public class FStaticReceiverData
{
    /** The receiving component. */
    public FPackageIndex Component;

    /** Source vertex data. */
    public FDecalVertex[] Vertices;

    /** Index buffer. */
    public short[] Indices;

    /** Number of decal triangles. */
    public int NumTriangles;

    /** Lightmap. */
    public FLightMap? LightMap1D;

    public FStaticReceiverData(FAssetArchive Ar)
    {
        Component = new FPackageIndex(Ar);
        Vertices = Ar.ReadBulkArray(() => new FDecalVertex(Ar));
        Indices = Ar.ReadBulkArray<short>();
        NumTriangles = Ar.Read<int>();

        if (Ar.Ver < EUnrealEngineObjectUE3Version.DECAL_ADDED_DECAL_VERTEX_LIGHTMAP_COORD) return;

        LightMap1D = Ar.Read<ELightMapType>() switch
        {
            ELightMapType.LMT_1D => new FLegacyLightMap1D(Ar),
            ELightMapType.LMT_2D => new FLightMap2D(Ar),
            _ => null
        };

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.DECAL_SHADOWMAPS)
        {
            Ar.SkipArray<int>(); // ShadowMap1D - PackageIndex
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.DECAL_SERIALIZE_BSP_ELEMENT)
        {
            Ar.Position += sizeof(int); // Data
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATIC_DECAL_INSTANCE_INDEX)
        {
            Ar.Position += sizeof(int); // InstanceIndex
        }
    }
}
