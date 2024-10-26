using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Meshes;

public class FMeshBufferSet
{
    public uint ElementCount;
    public FMeshBuffer[] Buffers;
    
    public FMeshBufferSet(FAssetArchive Ar)
    {
        ElementCount = Ar.Read<uint>();
        Buffers = Ar.ReadArray(() => new FMeshBuffer(Ar));
    }
}