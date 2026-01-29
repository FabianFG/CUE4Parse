using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public struct AkMediaMap
{
    public byte Index { get; set; }
    public uint SourceId { get; set; }

    public AkMediaMap(FArchive Ar)
    {
        Index = Ar.Read<byte>();
        SourceId = Ar.Read<uint>();
    }
}
