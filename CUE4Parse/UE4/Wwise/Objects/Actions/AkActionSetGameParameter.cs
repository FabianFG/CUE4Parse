using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetGameParameter
{
    public readonly ActionParams ActionParams;
    public readonly bool? BypassTransition;
    public readonly EAkValueMeaning ValueMeaning;
    public readonly RandomizerModifier RandomizerModifier;
    public readonly ExceptParams ExceptParams;

    public AkActionSetGameParameter(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);
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

        RandomizerModifier = new RandomizerModifier(Ar);
        ExceptParams = new ExceptParams(Ar);
    }
}
