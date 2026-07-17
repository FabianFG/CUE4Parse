namespace CUE4Parse.UE4.Wwise.Objects;

public sealed class AkFeedbackInfo
{
    public readonly uint BusId;

    public readonly float FeedbackVolume;
    public readonly float FeedbackModifierMin;
    public readonly float FeedbackModifierMax;
    public readonly float FeedbackLPF;
    public readonly float FeedbackLPFModMin;
    public readonly float FeedbackLPFModMax;

    public AkFeedbackInfo(FWwiseArchive Ar)
    {
        BusId = Ar.Read<uint>();
        if (Ar.Version <= 56 && BusId != 0)
        {
            FeedbackVolume = Ar.Read<float>();
            FeedbackModifierMin = Ar.Read<float>();
            FeedbackModifierMax = Ar.Read<float>();
            FeedbackLPF = Ar.Read<float>();
            FeedbackLPFModMin = Ar.Read<float>();
            FeedbackLPFModMax = Ar.Read<float>();
        }
    }
}
