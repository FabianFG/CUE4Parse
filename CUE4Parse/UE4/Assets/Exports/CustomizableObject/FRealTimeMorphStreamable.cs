using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public readonly struct FRealTimeMorphStreamable
{
    public readonly FName[] NameResolutionMap;
    public readonly FMutableStreamableBlock Block;
    public readonly uint Size;
    public readonly uint SourceId;
    
    public FRealTimeMorphStreamable(FArchive Ar)
    {
        NameResolutionMap = Ar.ReadArray(Ar.ReadFName);
        Block = Ar.Read<FMutableStreamableBlock>();
        Size = Ar.Read<uint>();
        SourceId = Ar.Read<uint>();
    }
}