using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class FPS3StaticMeshData
{
    public int[] IoBufferSize;
    public int[] ScratchBufferSize;
    public ushort[] CommandBufferHoleSize;
    public ushort[] IndexBias;
    public ushort[] VertexCount;
    public ushort[] TriangleCount;
    public ushort[] FirstVertex;
    public ushort[] FirstTriangle;

    public FPS3StaticMeshData(FArchive Ar)
    {
        IoBufferSize = Ar.ReadArray<int>();
        ScratchBufferSize = Ar.ReadArray<int>();
        CommandBufferHoleSize = Ar.ReadArray<ushort>();
        IndexBias = Ar.ReadArray<ushort>();
        VertexCount = Ar.ReadArray<ushort>();
        TriangleCount = Ar.ReadArray<ushort>();
        FirstVertex = Ar.ReadArray<ushort>();
        FirstTriangle = Ar.ReadArray<ushort>();
    }
}