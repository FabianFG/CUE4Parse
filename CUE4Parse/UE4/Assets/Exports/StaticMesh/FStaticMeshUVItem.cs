using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

[JsonConverter(typeof(FStaticMeshUVItemConverter))]
public class FStaticMeshUVItem
{
    public readonly FPackedNormal[] Normal;
    public readonly FMeshUVFloat[] UV;
    public readonly FVector Position;
    public readonly FColor Color;

    public FStaticMeshUVItem(FArchive Ar, bool useHighPrecisionTangents, int numStaticUVSets, bool useStaticFloatUVs)
    {
        if (Ar.Ver < EUnrealEngineObjectUE3Version.MovedColorFromUVItem)
        {
            Position = Ar.Read<FVector>();
            Color = Ar.Read<FColor>();
        }
        Normal = SerializeTangents(Ar, useHighPrecisionTangents);
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_VERTEXCOLOR && Ar.Ver < EUnrealEngineObjectUE3Version.MESH_PAINT_SYSTEM)
        {
            Color = Ar.Read<FColor>();
        }
        UV = SerializeTexcoords(Ar, numStaticUVSets, useStaticFloatUVs);
    }

    public FStaticMeshUVItem(FPackedNormal[] normal, FMeshUVFloat[] uv)
    {
        Normal = normal;
        UV = uv;
    }

    public static FPackedNormal[] SerializeTangents(FArchive Ar, bool useHighPrecisionTangents)
    {
        if (!useHighPrecisionTangents)
            return [new FPackedNormal(Ar), Ar.Ver < EUnrealEngineObjectUE3Version.AddedRemovedNormal ? new FPackedNormal(Ar) : new FPackedNormal(0), new FPackedNormal(Ar)]; // # TangentX, TangentY and TangentZ

        return [(FPackedNormal) new FPackedRGBA16N(Ar), Ar.Ver < EUnrealEngineObjectUE3Version.AddedRemovedNormal ? (FPackedNormal) new FPackedRGBA16N(Ar) : new FPackedNormal(0), (FPackedNormal) new FPackedRGBA16N(Ar)];
    }

    public static FMeshUVFloat[] SerializeTexcoords(FArchive Ar, int numStaticUVSets, bool useStaticFloatUVs)
    {
        if (useStaticFloatUVs)
        {
            return Ar.ReadArray<FMeshUVFloat>(numStaticUVSets);
        }

        var uvFloat = new FMeshUVFloat[numStaticUVSets];
        for (var i = 0; i < numStaticUVSets; i++)
        {
            uvFloat[i] = (FMeshUVFloat) Ar.Read<FMeshUVHalf>();
        }
        return uvFloat;
    }

    public static FPackedNormal[] SerializeHonorOfKingsWorldQTangent(FArchive Ar, bool useHighPrecisionTangents)
    {
        var qTangent = useHighPrecisionTangents ? UnpackQTangent(Ar.Read<TIntVector4<short>>()) : UnpackQTangent(Ar.Read<TIntVector4<sbyte>>());
        var orientation = qTangent.W < 0.0f ? -1.0f : 1.0f;
        qTangent.Normalize();

        var tangentX = qTangent * FVector.ForwardVector;
        var normal = qTangent * FVector.UpVector;
        var tangentZ = new FVector4(normal, orientation);

        return [new FPackedNormal(tangentX), new FPackedNormal(0), new FPackedNormal(tangentZ)];
    }

    private static FQuat UnpackQTangent(TIntVector4<sbyte> packed) => new(
        MathF.Max(packed.X / 127.0f, -1.0f), MathF.Max(packed.Y / 127.0f, -1.0f),
        MathF.Max(packed.Z / 127.0f, -1.0f), MathF.Max(packed.W / 127.0f, -1.0f));

    private static FQuat UnpackQTangent(TIntVector4<short> packed) => new(
        MathF.Max(packed.X / 32767.0f, -1.0f), MathF.Max(packed.Y / 32767.0f, -1.0f),
        MathF.Max(packed.Z / 32767.0f, -1.0f), MathF.Max(packed.W / 32767.0f, -1.0f));
}
