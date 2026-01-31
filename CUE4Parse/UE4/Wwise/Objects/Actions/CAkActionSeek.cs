using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSeek
{
    public readonly byte IsSeekRelativeToDuration;
    public readonly AkRandomizerModifier RandomizerModifier;
    public readonly byte SnapToNearestMarker;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionSeek::SetActionParams
    public CAkActionSeek(FArchive Ar)
    {
        IsSeekRelativeToDuration = Ar.Read<byte>();
        RandomizerModifier = new AkRandomizerModifier(Ar);
        SnapToNearestMarker = Ar.Read<byte>();
        ExceptParams = new CAkActionExcept(Ar);
    }
}
