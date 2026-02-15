using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMediaMap
{
    public readonly byte Index;
    public readonly uint SourceId;

    public AkMediaMap(FArchive Ar)
    {
        Index = Ar.Read<byte>();
        SourceId = Ar.Read<uint>();
    }
}
