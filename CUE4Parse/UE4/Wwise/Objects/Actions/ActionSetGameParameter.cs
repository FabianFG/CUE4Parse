using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class ActionSetGameParameter
{
    public ActionParams ActionParams { get; private set; }
    public bool? BypassTransition { get; private set; }
    public EValueMeaning ValueMeaning { get; private set; }
    public RangedParameter<float> Modifier { get; private set; }
    public ExceptParams ExceptParams { get; private set; }

    public ActionSetGameParameter(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);
        if (WwiseVersions.WwiseVersion > 89)
        {
            BypassTransition = Ar.Read<byte>() != 0;
        }

        if (WwiseVersions.WwiseVersion <= 56)
        {
            ValueMeaning = (EValueMeaning) Ar.Read<uint>();
        }
        else
        {
            ValueMeaning = (EValueMeaning) Ar.Read<byte>();
        }

        var modifier = new RangedParameter<float>
        {
            Base = Ar.Read<float>(),
            Min = Ar.Read<float>(),
            Max = Ar.Read<float>()
        };
        Modifier = modifier;
        ExceptParams = new ExceptParams(Ar);
    }

    public class RangedParameter<T>
    {
        public float Base { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
    }
}
