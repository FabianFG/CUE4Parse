using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSeek
{
    public byte IsSeekRelativeToDuration { get; protected set; }
    public RandomizerModifier RandomizerModifier { get; private set; }
    public byte SnapToNearestMarker { get; protected set; }
    public ExceptParams ExceptParams { get; private set; }

    public AkActionSeek(FArchive Ar)
    {
        IsSeekRelativeToDuration = Ar.Read<byte>();
        RandomizerModifier = new RandomizerModifier(Ar);
        SnapToNearestMarker = Ar.Read<byte>();
        ExceptParams = new ExceptParams(Ar);
    }
}
