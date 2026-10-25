using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class URailsStaticMesh : UStaticMesh;

public partial class FStaticMeshUVItem
{
    private enum ETangentPrecision : byte
    {
        PackedNormal,
        Unknown, // Maybe high precision?
        QTangent8,
        QTangent16,
    }

    public static FPackedNormal[][] SerializeTangentsGangstar(FArchive Ar)
    {
        var unknown = Ar.Read<byte>();
        var tangentPrecision = Ar.Read<ETangentPrecision>();
        _ = tangentPrecision != ETangentPrecision.PackedNormal ? Ar.Read<float>() : 0; // Tangent compression scale

        return tangentPrecision switch
        {
            ETangentPrecision.PackedNormal => [.. Ar.ReadBulkArray(() => SerializeTangents(Ar, false))],
            ETangentPrecision.QTangent8 => [.. Ar.ReadBulkArray<TIntVector4<sbyte>>().Select(DecodeQTangent)],
            ETangentPrecision.QTangent16 => [.. Ar.ReadBulkArray<TIntVector4<short>>().Select(DecodeQTangent)],
            _ => throw new ParserException(Ar, $"Unsupported Gangstar tangent precision {tangentPrecision}")
        };
    }

    private static FPackedNormal[] DecodeQTangent(TIntVector4<sbyte> packed) =>
        DecodeQTangent(new FQuat(packed.X, packed.Y, packed.Z, packed.W));
    private static FPackedNormal[] DecodeQTangent(TIntVector4<short> packed) =>
        DecodeQTangent(new FQuat(packed.X, packed.Y, packed.Z, packed.W));
    private static FPackedNormal[] DecodeQTangent(FQuat qTangent)
    {
        var orientation = qTangent.W < 0.0f ? -1.0f : 1.0f;
        qTangent.Normalize();

        var tangentX = qTangent * FVector.ForwardVector;
        var tangentZ = new FVector4(qTangent * FVector.UpVector, orientation);
        return [new FPackedNormal(tangentX), new FPackedNormal(0), new FPackedNormal(tangentZ)];
    }
}
