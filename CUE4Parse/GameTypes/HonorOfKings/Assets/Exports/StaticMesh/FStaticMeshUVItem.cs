using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public partial class FStaticMeshUVItem
{
    public static FPackedNormal[] SerializeHonorOfKingsWorldQTangent(FArchive Ar, bool useHighPrecisionTangents)
    {
        var qTangent = useHighPrecisionTangents ? UnpackQTangent(Ar.Read<TIntVector4<short>>()) : UnpackQTangent(Ar.Read<TIntVector4<sbyte>>());
        var orientation = qTangent.W < 0.0f ? -1.0f : 1.0f;
        qTangent.W = MathF.Abs(qTangent.W);
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
