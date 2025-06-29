using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionPlay
{
    public readonly ActionParams ActionParams;
    public readonly uint? BankId;
    public readonly uint? BankType;

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
