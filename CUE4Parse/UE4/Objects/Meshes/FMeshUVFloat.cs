using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Meshes;

[StructLayout(LayoutKind.Sequential)]
public struct FMeshUVFloat : IUStruct
{
    public static readonly FMeshUVFloat ZeroVector = new(0, 0);
    public static readonly FMeshUVFloat OneVector = new(1, 1);

    public float U;
    public float V;

    public FMeshUVFloat(float u, float v)
    {
        U = u;
        V = v;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(U);
        Ar.Write(V);
    }

    public static FMeshUVFloat operator +(FMeshUVFloat a, FMeshUVFloat b) => new(a.U + b.U, a.V + b.V);
    public static FMeshUVFloat operator -(FMeshUVFloat a, FMeshUVFloat b) => new(a.U - b.U, a.V - b.V);
    public static FMeshUVFloat operator *(FMeshUVFloat a, FMeshUVFloat b) => new(a.U * b.U, a.V * b.V);
    public static FMeshUVFloat operator /(FMeshUVFloat a, FMeshUVFloat b) => new(a.U / b.U, a.V / b.V);

    public static FMeshUVFloat operator +(FMeshUVFloat a, float b) => new(a.U + b, a.V + b);
    public static FMeshUVFloat operator *(FMeshUVFloat a, float b) => new(a.U * b, a.V * b);
    public static FMeshUVFloat operator -(FMeshUVFloat a, float b) => new(a.U - b, a.V - b);

    public override string ToString() => $"U={U,3:F3} V={V,3:F3}";

    public static explicit operator Vector2(FMeshUVFloat uv) => new(uv.U, uv.V);
    public static explicit operator FMeshUVFloat(Vector2 uv) => new(uv.X, uv.Y);
    public static explicit operator FMeshUVFloat(FVector2D uv) => new(uv.X, uv.Y);

    public static explicit operator FMeshUVFloat(FMeshUVHalf uvHalf) => new((float)uvHalf.U, (float)uvHalf.V);
}
