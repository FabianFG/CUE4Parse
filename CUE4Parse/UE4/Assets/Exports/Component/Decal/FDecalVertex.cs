using CUE4Parse.UE4;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Decal;

public class FDecalVertex : IUStruct
{
    public FVector Position;
    public FPackedNormal? TangentX;
    public FPackedNormal? TangentY;
    public FPackedNormal TangentZ;

    /** Decal mesh texture coordinates. */
    public FVector2D UV;

    /** Decal vertex light map coordinated. */
    public FVector2D LightMapCoordinate;

    /** Transforms receiver tangent basis into decal tangent basis for normal map lookup. */
    public FVector2D NormalTransform0;
    public FVector2D NormalTransform1;

    public FDecalVertex(FArchive Ar)
    {
        Position = Ar.Read<FVector>();

        if (Ar.Ver < EUnrealEngineObjectUE3Version.DECAL_VERTEX_FACTORY_VER1 ||
            Ar.Ver >= EUnrealEngineObjectUE3Version.DECAL_VERTEX_FACTORY_VER2)
        {
            TangentX = new FPackedNormal(Ar);
        }

        if (Ar.Ver < EUnrealEngineObjectUE3Version.SKELETAL_MESH_REMOVE_BINORMAL_TANGENT_VECTOR)
        {
            TangentY = new FPackedNormal(Ar);
        }

        TangentZ = new FPackedNormal(Ar);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.DECAL_VERTEX_FACTORY_VER1)
        {
            UV = Ar.Read<FVector2D>();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.DECAL_ADDED_DECAL_VERTEX_LIGHTMAP_COORD)
        {
            LightMapCoordinate = Ar.Read<FVector2D>();
        }

        if (Ar.Ver < EUnrealEngineObjectUE3Version.DECAL_REMOVED_2X2_NORMAL_TRANSFORM)
        {
            NormalTransform0 = Ar.Read<FVector2D>();
            NormalTransform1 = Ar.Read<FVector2D>();
        }
    }
}
