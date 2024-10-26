using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Meshes;

public class FMeshBuffer
{
    public FMeshBufferChannel[] Channels;
    public byte[] Data;
    public uint ElementSize;

    public FMeshBuffer(FAssetArchive Ar)
    {
        Channels = Ar.ReadArray(() => new FMeshBufferChannel(Ar));
        Data = Ar.ReadArray<byte>();
        ElementSize = Ar.Read<uint>();
    }
}