using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicFade
{
    public readonly int TransitionTime;
    public readonly EAkCurveInterpolation FadeCurve;
    public readonly int FadeOffset;

    public AkMusicFade(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = (EAkCurveInterpolation)Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();
    }
}
