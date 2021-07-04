using CUE4Parse.UE4.Readers;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Meshes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FMeshUVHalf
    {
        public readonly ushort U;
        public readonly ushort V;

        public FMeshUVHalf(FArchive Ar)
        {
            U = Ar.Read<ushort>();
            V = Ar.Read<ushort>();
        }

        public FMeshUVHalf(ushort u, ushort v)
        {
            U = u;
            V = v;
        }
    }
}
