using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ActionPlay
{
    public uint? BankId { get; private set; }
    public uint? BankType { get; private set; }

    public ActionPlay(FArchive Ar)
    {
        if (WwiseVersions.WwiseVersion > 26)
        {
            BankId = Ar.Read<uint>();
        }

        if (WwiseVersions.WwiseVersion >= 144)
        {
            BankType = Ar.Read<uint>();
        }
    }
}
