using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionBase(FArchive Ar)
{
    public ActionParams ActionParams { get; private set; } = new ActionParams(Ar);
    public ExceptParams ExceptParams { get; private set; } = new ExceptParams(Ar);
}
