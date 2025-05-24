using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSeek
{
    public readonly byte IsSeekRelativeToDuration;
    public readonly RandomizerModifier RandomizerModifier;
    public readonly byte SnapToNearestMarker;
    public readonly ExceptParams ExceptParams;

    public AkActionSeek(FArchive Ar)
    {
        IsSeekRelativeToDuration = Ar.Read<byte>();
        RandomizerModifier = new RandomizerModifier(Ar);
        SnapToNearestMarker = Ar.Read<byte>();
        ExceptParams = new ExceptParams(Ar);
    }
}
