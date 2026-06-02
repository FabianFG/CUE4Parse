namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionBase(FWwiseArchive Ar)
{
    public readonly CAkActionParams ActionParams = new(Ar);
    public readonly CAkActionExcept ExceptParams = new(Ar);
}
