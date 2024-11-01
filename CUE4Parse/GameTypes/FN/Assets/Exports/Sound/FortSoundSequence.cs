using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Sound;

public class UFortSoundSequence : UObject
{
    public FSoundSequenceData[] SoundSequenceData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (TryGetValue(out FStructFallback[] soundSequenceData, nameof(SoundSequenceData)))
        {
            SoundSequenceData = new FSoundSequenceData[soundSequenceData.Length];
            for (var i = 0; i < SoundSequenceData.Length; i++)
            {
                SoundSequenceData[i] = new FSoundSequenceData(soundSequenceData[i]);
            }
        }
    }
}

public class FSoundSequenceData
{
    public USoundCue Sound;
    public float Delay;

    public FSoundSequenceData(FStructFallback fallback)
    {
        Sound = fallback.GetOrDefault<USoundCue>(nameof(Sound));
        Delay = fallback.GetOrDefault<float>(nameof(Delay));
    }
}