using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkTransParams
{
    public readonly AkFadeParams SourceFadeParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ESyncType SyncType;
    public readonly uint CueFilterHash;
    public readonly AkFadeParams DestinationFadeParams;

    public AkTransParams(FArchive Ar)
    {
        SourceFadeParams = new AkFadeParams(Ar);
        SyncType = Ar.Read<ESyncType>();
        CueFilterHash = Ar.Read<uint>();
        DestinationFadeParams = new AkFadeParams(Ar);
    }

    public class AkFadeParams
    {
        public readonly uint TransitionTime;
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly ECurveInterpolation FadeCurve;
        public readonly uint FadeOffset;

        public AkFadeParams(FArchive Ar)
        {
            TransitionTime = Ar.Read<uint>();
            var fadeCurve = Ar.Read<uint>();
            FadeCurve = (ECurveInterpolation) (byte) fadeCurve;
            FadeOffset = Ar.Read<uint>();
        }
    }
}
