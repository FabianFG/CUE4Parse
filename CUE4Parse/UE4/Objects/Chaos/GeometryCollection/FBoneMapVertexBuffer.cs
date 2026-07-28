using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FBoneMapVertexBuffer
{
    public uint NumVertices;
    public ushort[] BoneMap;

    public FBoneMapVertexBuffer(FArchive Ar)
    {
        NumVertices = Ar.Read<uint>();
        BoneMap = Ar.ReadBulkArray<ushort>();
    }
}
