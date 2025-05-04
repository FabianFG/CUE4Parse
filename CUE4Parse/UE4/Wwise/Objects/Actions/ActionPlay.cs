using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ActionPlay
{
    public int? TTime { get; private set; }
    public int? TTimeMin { get; private set; }
    public int? TTimeMax { get; private set; }
    public byte ByBitVector { get; private set; }
    public uint? BankId { get; private set; }
    public uint? BankType { get; private set; }

    public ActionPlay(FArchive Ar)
    {
        if (WwiseVersions.WwiseVersion <= 56)
        {
            TTime = Ar.Read<int>();
            TTimeMin = Ar.Read<int>();
            TTimeMax = Ar.Read<int>();
        }

        ByBitVector = Ar.Read<byte>();

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
