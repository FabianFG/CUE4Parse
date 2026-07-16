using CUE4Parse.GameTypes.LegoBatman.Assets;

namespace CUE4Parse.UE4.Wwise;

public partial class WwiseProvider
{
    public List<WwiseExtractedSound> ExtractWubAudioEventSounds(UWubAudioEvent wubAudioEvent)
    {
        var results = new List<WwiseExtractedSound>();

        if (string.IsNullOrEmpty(wubAudioEvent?.AudioEventName))
            return results;
        var audioEventId = WwiseFnv.GetHash(wubAudioEvent.AudioEventName);
        LoopThroughEvent(audioEventId, results, GetOwnerDirectory(wubAudioEvent), wubAudioEvent.AudioEventName);

        return results;
    }
}
