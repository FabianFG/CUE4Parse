using System.Numerics;
using System.Runtime.InteropServices;

using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Meshes;

[StructLayout(LayoutKind.Sequential)]
public struct FMeshUVFloat : IUStruct
{
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

    public static explicit operator Vector2(FMeshUVFloat uv) => new(uv.U, uv.V);
    public static explicit operator FMeshUVFloat(Vector2 uv) => new(uv.X, uv.Y);
    public static explicit operator FMeshUVFloat(FVector2D uv) => new(uv.X, uv.Y);
        
    public static explicit operator FMeshUVFloat(FMeshUVHalf uvHalf) => new((float)uvHalf.U, (float)uvHalf.V);
}
