using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionPlay
{
    public readonly CAkActionParams ActionParams;
    public readonly uint? BankId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkBankTypeEnum BankType;

    // CAkActionPlay::SetActionParams
    public CAkActionPlay(FArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);
        if (WwiseVersions.Version > 26)
        {
            BankId = Ar.Read<uint>();
        }

        if (WwiseVersions.Version >= 144)
        {
            BankType = Ar.Read<EAkBankTypeEnum>();
        }
    }
}
