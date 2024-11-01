using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Meshes;

public class FMeshBuffer
{
    public FMeshBufferChannel[] Channels;
    public byte[] Data;
    public uint ElementSize;

    public FMeshBuffer(FArchive Ar)
    {
        Channels = Ar.ReadArray(() => new FMeshBufferChannel(Ar));
        Data = Ar.ReadArray<byte>();
        ElementSize = Ar.Read<uint>();
    }
}