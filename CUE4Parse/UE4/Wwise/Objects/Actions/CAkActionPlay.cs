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
    public CAkActionPlay(FWwiseArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);
        if (Ar.Version > 26)
        {
            BankId = Ar.Read<uint>();
        }

        if (Ar.Version >= 144)
        {
            BankType = Ar.Read<EAkBankTypeEnum>();
        }
    }
}
