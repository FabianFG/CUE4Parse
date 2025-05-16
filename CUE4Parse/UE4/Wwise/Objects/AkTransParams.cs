using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkTransParams
{
    public AkFadeParams SourceFadeParams { get; private set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ESyncType SyncType { get; private set; }
    public uint CueFilterHash { get; private set; }
    public AkFadeParams DestinationFadeParams { get; private set; }

    public AkTransParams(FArchive Ar)
    {
        SourceFadeParams = new AkFadeParams(Ar);
        SyncType = Ar.Read<ESyncType>();
        CueFilterHash = Ar.Read<uint>();
        DestinationFadeParams = new AkFadeParams(Ar);
    }

    public class AkFadeParams
    {
        public uint TransitionTime { get; private set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ECurveInterpolation FadeCurve { get; private set; }
        public uint FadeOffset { get; private set; }

        public AkFadeParams(FArchive Ar)
        {
            TransitionTime = Ar.Read<uint>();
            var fadeCurve = Ar.Read<uint>();
            FadeCurve = (ECurveInterpolation)(byte)fadeCurve;
            FadeOffset = Ar.Read<uint>();
        }
    }

}
