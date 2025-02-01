using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FMutableStreamableBlock
{
    public uint FileId;
    public uint Flags;
    public ulong Offset;
    
    public FMutableStreamableBlock(FArchive Ar)
    {
        FileId = Ar.Read<uint>();
        Flags = Ar.Read<uint>();
        Offset = Ar.Read<ulong>();
    }
}