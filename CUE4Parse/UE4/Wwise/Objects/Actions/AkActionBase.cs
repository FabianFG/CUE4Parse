using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionBase(FArchive Ar)
{
    public readonly ActionParams ActionParams = new(Ar);
    public readonly ExceptParams ExceptParams = new(Ar);
}
