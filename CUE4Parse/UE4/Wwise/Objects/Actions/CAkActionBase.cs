using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionBase(FArchive Ar)
{
    public readonly CAkActionParams ActionParams = new(Ar);
    public readonly CAkActionExcept ExceptParams = new(Ar);
}
