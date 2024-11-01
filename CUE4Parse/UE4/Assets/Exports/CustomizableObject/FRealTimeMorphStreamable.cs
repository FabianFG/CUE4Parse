using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FRealTimeMorphStreamable
{
    public FName[] NameResolutionMap;
    public uint Size;
    public FMutableStreamableBlock Block;
    public uint SourceId;
    
    public FRealTimeMorphStreamable(FArchive Ar)
    {
        NameResolutionMap = Ar.ReadArray(Ar.ReadFName);
        Size = Ar.Read<uint>();
        Block = new FMutableStreamableBlock(Ar);
        SourceId = Ar.Read<uint>();
    }
}