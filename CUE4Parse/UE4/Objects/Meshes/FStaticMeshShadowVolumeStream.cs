using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Meshes;

public class FStaticMeshShadowVolumeStream
{
    public readonly float[] VertexData;
    public readonly int Stride;
    public readonly int NumVertices;

    public FStaticMeshShadowVolumeStream()
    {
        VertexData = [];
    }

    public FStaticMeshShadowVolumeStream(FArchive Ar)
    {
        Stride = Ar.Read<int>();
        NumVertices = Ar.Read<int>();

        if (NumVertices > 0)
        {
            VertexData = Ar.ReadBulkArray<float>();
        }
        else
        {
            VertexData = [];
        }
    }
}
