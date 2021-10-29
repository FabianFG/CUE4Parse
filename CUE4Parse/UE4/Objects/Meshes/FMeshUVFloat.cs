using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using static CUE4Parse.Utils.TypeConversionUtils;

namespace CUE4Parse.UE4.Objects.Meshes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FMeshUVFloat : IUStruct
    {
        public float U;
        public float V;

        public FMeshUVFloat(FArchive Ar)
        {
            U = Ar.Read<float>();
            V = Ar.Read<float>();
        }

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

        public static implicit operator Vector2(FMeshUVFloat uv) => new(uv.U, uv.V);

        public static explicit operator FMeshUVFloat(FMeshUVHalf uvHalf)
        {
            return new(HalfToFloat(uvHalf.U), HalfToFloat(uvHalf.V));
        }
    }
}