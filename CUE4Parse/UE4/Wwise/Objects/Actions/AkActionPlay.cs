using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionPlay
{
    public ActionParams ActionParams { get; private set; }
    public uint? BankId { get; private set; }
    public uint? BankType { get; private set; }

    public AkActionPlay(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);
        if (WwiseVersions.Version > 26)
        {
            BankId = Ar.Read<uint>();
        }

        if (WwiseVersions.Version >= 144)
        {
            BankType = Ar.Read<uint>();
        }
    }
}
