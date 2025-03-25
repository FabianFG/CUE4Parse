using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Meshes;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FMeshUVHalf : IUStruct
{
    public readonly Half U;
    public readonly Half V;

    public FMeshUVHalf(Half u, Half v)
    {
        U = u;
        V = v;
    }
}
