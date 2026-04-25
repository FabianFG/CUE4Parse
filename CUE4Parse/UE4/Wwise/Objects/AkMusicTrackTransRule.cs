using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicTrackTransRule
{
    public readonly AkMusicFade SourceFadeParams;
    public readonly EAkSyncType SyncType;
    public readonly uint CueFilterHash;
    public readonly AkMusicFade DestinationFadeParams;

    public AkMusicTrackTransRule(FWwiseArchive Ar)
    {
        SourceFadeParams = new AkMusicFade(Ar);
        SyncType = Ar.Read<EAkSyncType>();
        CueFilterHash = Ar.Read<uint>();
        DestinationFadeParams = new AkMusicFade(Ar);
    }
}
