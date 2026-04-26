namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMediaMap
{
    public readonly byte Index;
    public readonly uint SourceId;

    public AkMediaMap(FWwiseArchive Ar)
    {
        Index = Ar.Read<byte>();
        SourceId = Ar.Read<uint>();
    }
}
