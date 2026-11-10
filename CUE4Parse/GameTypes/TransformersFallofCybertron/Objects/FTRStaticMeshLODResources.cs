using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.TransformersFallofCybertron.Objects;

public static class FTRStaticMeshLODResources
{
    public static void SerializeBuffersLegacy(FArchive Ar, FStaticMeshLODResources lod)
    {
        lod.PositionVertexBuffer = new FPositionVertexBuffer(Ar);
        lod.VertexBuffer = new FStaticMeshVertexBuffer(Ar);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDITIONAL_COOK_PACKAGE_SUMMARY)
        {
            lod.ColorVertexBuffer = new FColorVertexBuffer(Ar);
        }

        if (Ar.LicenseeVer >= 71)
        {
            new FTRMeshUnkStream(Ar);
        }

        if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_PREFAB_SEQUENCES)
        {
            // FStaticMeshShadowVolumeStream - ShadowExtrusionVertexBuffer (FColorVertexBuffer but uses floats)
            Ar.Position += sizeof(int); // Stride
            if (Ar.Read<int>() > 0) Ar.SkipBulkArrayData(sizeof(float)); // NumVertices, VertexData
        }

        lod.NumVertices = Ar.Read<int>();
        lod.IndexBuffer = new FRawStaticIndexBuffer(Ar);
        lod.WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (Ar.LicenseeVer >= 58) Ar.Position += sizeof(int);
        if (Ar.LicenseeVer >= 181) Ar.Position += sizeof(int);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_PREFAB_SEQUENCES)
        {
            Ar.SkipBulkArrayData(16); // Edges
            Ar.SkipArray<byte>();
        }
    }
}
