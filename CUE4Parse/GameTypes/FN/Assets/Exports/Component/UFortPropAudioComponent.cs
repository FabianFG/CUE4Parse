using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Component;

public class UFortPropAudioComponent : UAudioComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Sound ??= GetOrDefault<USoundBase?>("SoundLoop");
    }
}