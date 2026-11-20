using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Decal;

public class FDecalRenderData
{
    public FDecalRenderData(FAssetArchive Ar)
    {
        Ar.ReadBulkArray(() => new FDecalVertex(Ar));
        Ar.SkipBulkArrayData(2);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.RENDERING_REFACTOR)
        {
            Ar.Position += sizeof(int); // LegacySize
            new FRawStaticIndexBuffer(Ar);
            Ar.Position += sizeof(int); // NumTriangles
        }

        if (Ar.Ver == EUnrealEngineObjectUE3Version.DECAL_RENDERDATA)
        {
            Ar.Position += sizeof(int); // LightMap - PackageIndex
        }
    }
}
