using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicFade
{
    public readonly int TransitionTime;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ECurveInterpolation FadeCurve;
    public readonly int FadeOffset;

    public AkMusicFade(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = (ECurveInterpolation)Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();
    }
}
