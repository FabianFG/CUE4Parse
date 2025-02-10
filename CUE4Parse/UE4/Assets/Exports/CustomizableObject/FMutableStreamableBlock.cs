using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FMutableStreamableBlock
{
    public uint FileId;
    public uint Flags;
    public ulong Offset;

    public FMutableStreamableBlock(FAssetArchive Ar)
    {
        FileId = Ar.Read<uint>();
        Flags = Ar.Read<uint>();
        Offset = Ar.Read<ulong>();
    }
}
