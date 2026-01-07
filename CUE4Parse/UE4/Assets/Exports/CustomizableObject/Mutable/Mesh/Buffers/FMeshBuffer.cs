using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Buffers;

public class FMeshBuffer
{
    public FMeshBufferChannel[] Channels;
    public byte[] Data;
    public uint ElementSize;
    
    public FMeshBuffer(FMutableArchive Ar)
    {
        Channels = Ar.ReadArray<FMeshBufferChannel>();
        Data = Ar.ReadArray<byte>();
        ElementSize = Ar.Read<uint>();
    }
}