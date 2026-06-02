using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSetAkProp
{
    public readonly CAkActionParams ActionParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkValueMeaning ValueMeaning;
    public readonly AkRandomizerModifier RandomizerModifier;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionSetAkProp::SetActionSpecificParams
    public CAkActionSetAkProp(FWwiseArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);

        if (Ar.Version <= 56)
        {
            ValueMeaning = (EAkValueMeaning) Ar.Read<uint>();
        }
        else
        {
            ValueMeaning = (EAkValueMeaning) Ar.Read<byte>();
        }

        RandomizerModifier = new AkRandomizerModifier(Ar);
        ExceptParams = new CAkActionExcept(Ar);
    }
}
