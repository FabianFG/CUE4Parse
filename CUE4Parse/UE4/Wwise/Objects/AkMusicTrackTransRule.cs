using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicTrackTransRule
{
    public readonly AkMusicFade SourceFadeParams;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ESyncType SyncType;
    public readonly uint CueFilterHash;
    public readonly AkMusicFade DestinationFadeParams;

    public AkMusicTrackTransRule(FArchive Ar)
    {
        SourceFadeParams = new AkMusicFade(Ar);
        SyncType = Ar.Read<ESyncType>();
        CueFilterHash = Ar.Read<uint>();
        DestinationFadeParams = new AkMusicFade(Ar);
    }
}
