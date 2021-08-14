using System;
using CUE4Parse.UE4.Readers;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

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

        public static explicit operator FMeshUVFloat(FMeshUVHalf uvHalf)
        {
            return new(Half2Float(uvHalf.U), Half2Float(uvHalf.V));
        }

        private static float Half2Float(ushort h)
        {
            var sign = (h >> 15) & 0x00000001;
            var exp  = (h >> 10) & 0x0000001F;
            var mant =  h        & 0x000003FF;

            exp += 127 - 15;
            return BitConverter.ToSingle(BitConverter.GetBytes((sign << 31) | (exp << 23) | (mant << 13)));
        }
    }
}