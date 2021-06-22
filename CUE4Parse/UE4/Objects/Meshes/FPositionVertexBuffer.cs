using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Meshes
{
    public class FPositionVertexBuffer
    {
        public readonly FVector[] Verts;
        public readonly int Stride;
        public readonly int NumVertices;

        public FPositionVertexBuffer(FArchive Ar)
        {
            Stride = Ar.Read<int>();
            NumVertices = Ar.Read<int>();
            Verts = Ar.ReadBulkArray<FVector>();
        }
    }
}
