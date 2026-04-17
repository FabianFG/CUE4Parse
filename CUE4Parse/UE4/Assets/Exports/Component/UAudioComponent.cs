using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UAudioComponent : USceneComponent
{
    public USoundBase? Sound { get; protected set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Sound = GetOrDefault<USoundBase?>(nameof(Sound));
    }
}
