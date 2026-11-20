using CUE4Parse.GameTypes.TransformersFallofCybertron.Objects;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public partial class FStaticMeshLODResources
{
    public void SerializeTransformersFallofCybertronBuffersLegacy(FArchive Ar)
    {
        PositionVertexBuffer = new FPositionVertexBuffer(Ar);
        VertexBuffer = new FStaticMeshVertexBuffer(Ar);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDITIONAL_COOK_PACKAGE_SUMMARY)
        {
            ColorVertexBuffer = new FColorVertexBuffer(Ar);
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

        NumVertices = Ar.Read<int>();
        IndexBuffer = new FRawStaticIndexBuffer(Ar);
        WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (Ar.LicenseeVer >= 58) Ar.Position += sizeof(int);
        if (Ar.LicenseeVer >= 181) Ar.Position += sizeof(int);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_PREFAB_SEQUENCES)
        {
            Ar.SkipBulkArrayData(16); // Edges
            Ar.SkipArray<byte>();
        }
    }
}
