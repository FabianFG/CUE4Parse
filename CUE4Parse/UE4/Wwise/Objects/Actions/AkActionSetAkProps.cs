using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetAkProps
{
    public readonly ActionParams ActionParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EValueMeaning ValueMeaning;
    public readonly RandomizerModifier RandomizerModifier;
    public readonly ExceptParams ExceptParams;

    public AkActionSetAkProps(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);

        if (WwiseVersions.Version <= 56)
        {
            ValueMeaning = (EValueMeaning) Ar.Read<uint>();
        }
        else
        {
            ValueMeaning = (EValueMeaning) Ar.Read<byte>();
        }

        RandomizerModifier = new RandomizerModifier(Ar);
        ExceptParams = new ExceptParams(Ar);
    }
}
