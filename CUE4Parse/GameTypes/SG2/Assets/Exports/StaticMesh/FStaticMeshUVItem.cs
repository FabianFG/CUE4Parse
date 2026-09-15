using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public partial class FStaticMeshUVItem
{
    public static FPackedNormal[][] SerializeTangents1047(FArchive Ar, bool useHighPrecisionTangents, int numVertices)
    {
        var itemSize = Ar.Read<int>();
        var itemCount = Ar.Read<int>();
        var position = Ar.Position;
        if (itemCount < 0 || itemCount != numVertices)
            throw new ParserException(Ar, $"Tangent count {itemCount} does not match vertex count {numVertices}");

        var quaternionTangents = itemSize == 4 && !useHighPrecisionTangents;
        var standardSize = useHighPrecisionTangents ? 16 : 8;
        if (!quaternionTangents && itemSize != standardSize)
            throw new ParserException(Ar, $"Unsupported 1047 tangent format: {itemSize} bytes per vertex; standard reader expects {standardSize}.");

        var payloadSize = (long) itemSize * itemCount;
        if (payloadSize > Ar.Length - position)
            throw new ParserException(Ar, $"Tangent payload requires {payloadSize} bytes, but only {Ar.Length - position} remain");

        var tangents = Ar.ReadArray(itemCount, () => quaternionTangents
            ? ReadQuaternionTangent1047(Ar)
            : SerializeTangents(Ar, useHighPrecisionTangents));
        if (Ar.Position - position != payloadSize)
            throw new ParserException(Ar, $"Read incorrect tangent payload size: {Ar.Position - position}, expected {payloadSize}");
        return tangents;
    }

    // Observed 1047 low-precision tangent records contain four signed-byte quaternion
    // components. Use the quaternion convention already supported by the Gangstar
    // decoder: signed W retains the tangent-frame handedness, and the full quaternion
    // is normalized before rotating the X/Z basis. Visual validation is still needed.
    private static FPackedNormal[] ReadQuaternionTangent1047(FArchive Ar)
    {
        var packed = Ar.Read<TIntVector4<sbyte>>();
        var tangent = new FQuat(packed.X, packed.Y, packed.Z, packed.W);
        var orientation = tangent.W < 0.0f ? -1.0f : 1.0f;
        tangent.Normalize();
        var tangentX = tangent * FVector.ForwardVector;
        var tangentZ = new FVector4(tangent * FVector.UpVector, orientation);
        return [new FPackedNormal(tangentX), new FPackedNormal(0), new FPackedNormal(tangentZ)];
    }

}
