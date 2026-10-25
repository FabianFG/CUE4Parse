using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

// Based on shader_decode.py from FFVII-Rebirth-Mesh-Patcher:
// https://github.com/nikolaybutnik/FFVII-Rebirth-Mesh-Patcher/blob/main/lib/shader_decode.py
// Copyright (c) 2026 nikolaybutnik. Licensed under the MIT License.
public partial class FStaticMeshUVItem
{
    private const uint ComponentMask = 0x3FF;
    private const uint NormalSignMask = 1u << 30;
    private const uint OrientationMask = 1u << 31;

    public static FPackedNormal[] SerializeTangentsFF7R(FArchive Ar)
    {
        var packed = Ar.Read<uint>();
        var normal = DecodeNormal(packed);
        var tangent = DecodeTangent(packed, normal);
        var orientation = (packed & OrientationMask) != 0 ? 1.0f : -1.0f;

        return [new FPackedNormal(tangent), new FPackedNormal(0), new FPackedNormal(new FVector4(normal, orientation))];
    }

    private static FVector DecodeNormal(uint packed)
    {
        var u = (packed & ComponentMask) / (float) ComponentMask;
        var v = (packed >> 10 & ComponentMask) / (float) ComponentMask;
        var x = u - v;
        var y = u + v - 1.0f;
        var z = 1.0f - MathF.Abs(x) - MathF.Abs(y);

        return new FVector(x, y, (packed & NormalSignMask) != 0 ? z : -z).GetSafeNormal();
    }

    private static FVector DecodeTangent(uint packed, FVector normal)
    {
        var sign = normal.Z >= 0.0f ? -1.0f : 1.0f;
        var a = 1.0f / (normal.Z - sign);
        var tangentBasisX = new FVector(1.0f + sign * normal.X * normal.X * a, sign * normal.X * normal.Y * a, sign * normal.X);
        var tangentBasisY = new FVector(normal.X * normal.Y * a, sign + normal.Y * normal.Y * a, normal.Y);

        var angle = packed >> 20 & ComponentMask;
        var t = (angle & 0xFF) / 255.0f;
        var direction = new FVector((angle & 0x100) != 0 ? t : -t, (angle & 0x200) != 0 ? 1.0f - t : -(1.0f - t), 0.0f).GetSafeNormal();

        return (tangentBasisX * direction.X + tangentBasisY * direction.Y).GetSafeNormal();
    }
}
