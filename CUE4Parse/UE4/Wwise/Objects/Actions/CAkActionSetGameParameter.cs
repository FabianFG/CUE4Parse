using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSetGameParameter
{
    public readonly CAkActionParams ActionParams;
    public readonly bool? BypassTransition;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkValueMeaning ValueMeaning;
    public readonly AkRandomizerModifier RandomizerModifier;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionSetGameParameter::SetActionSpecificParams
    public CAkActionSetGameParameter(FArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);
        if (WwiseVersions.Version > 89)
        {
            BypassTransition = Ar.Read<byte>() != 0;
        }

        if (WwiseVersions.Version <= 56)
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
