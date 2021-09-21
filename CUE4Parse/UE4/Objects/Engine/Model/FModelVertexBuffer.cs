using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Model
{
    public class FModelVertexBuffer
    {
        public readonly FModelVertex[] Vertices;

        public FModelVertexBuffer(FArchive Ar)
        {
            Vertices = Ar.ReadArray(() => new FModelVertex(Ar));
        }
    }    
}