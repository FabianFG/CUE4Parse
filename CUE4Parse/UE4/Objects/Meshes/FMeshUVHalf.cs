using CUE4Parse.UE4.Readers;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Meshes
{
    [StructLayout(LayoutKind.Sequential)]
    public class FMeshUVFloat : IUStruct
    {
        public readonly float U;
        public readonly float V;

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

        public static explicit operator FMeshUVFloat(FMeshUVHalf uvHalf)
        {
            return new (uvHalf.U, uvHalf.V);
        }
    }
}
