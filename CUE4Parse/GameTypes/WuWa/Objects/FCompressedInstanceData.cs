using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.WuWa.Objects;

/// <summary>WuWa's compact CPU instances, before custom/render data and the HISM cluster tree.</summary>
public static class FCompressedInstanceData
{
    public static FInstancedStaticMeshInstanceData[] Read(FArchive ar, long validPos)
    {
        if (validPos - ar.Position < 16)
            throw new ParserException(ar, "Truncated compressed instance header");
        var origin = ar.Read<FVector>();
        var count = ar.Read<int>();
        // Minimum record: half translation XYZ, half Euler XYZ, byte uniform-scale flag, half scale.
        if (count < 0 || count > (validPos - ar.Position) / 15)
            throw new ParserException(ar, $"Invalid compressed instance count {count}");
        Check(origin);
        var instances = new FInstancedStaticMeshInstanceData[count];
        for (var i = 0; i < count; i++)
        {
            if (validPos - ar.Position < 15)
                throw new ParserException(ar, "Truncated compressed instance");
            FVector translation = ar.Read<FHalfVector>();
            FVector angles = ar.Read<FHalfVector>();
            var uniform = ar.ReadFlag();
            if (validPos - ar.Position < (uniform ? 2 : 6))
                throw new ParserException(ar, "Truncated instance scale");
            FVector scale = uniform ? new FVector((float) ar.Read<Half>()) : ar.Read<FHalfVector>();
            Check(translation);
            Check(angles);
            Check(scale);
            // Euler XYZ stores roll, pitch, yaw in radians.
            const float toDegrees = 180f / MathF.PI;
            var rotation = new FRotator(angles.Y * toDegrees, angles.Z * toDegrees, angles.X * toDegrees);
            instances[i] = new FInstancedStaticMeshInstanceData(new FTransform(rotation, origin + translation, scale));
        }
        return instances;

        void Check(FVector v)
        {
            if (!float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z))
                throw new ParserException(ar, "Nonfinite compressed instance transform");
        }
    }
}
