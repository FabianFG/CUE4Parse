using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetAkProps
{
    public ActionParams ActionParams { get; private set; }
    public EValueMeaning ValueMeaning { get; private set; }
    public RandomizerModifier RandomizerModifier { get; private set; }
    public ExceptParams ExceptParams { get; private set; }

    public AkActionSetAkProps(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);

        if (WwiseVersions.WwiseVersion <= 56)
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
