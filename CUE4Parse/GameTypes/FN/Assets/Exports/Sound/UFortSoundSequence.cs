using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Sound;

public class UFortSoundSequence : UDataAsset
{
    public FSoundSequenceData[] SoundSequenceData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SoundSequenceData = GetOrDefault<FSoundSequenceData[]>(nameof(SoundSequenceData), []);
    }
}
