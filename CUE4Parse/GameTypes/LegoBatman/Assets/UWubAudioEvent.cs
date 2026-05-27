using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.LegoBatman.Assets;

public class UWubAudioEvent : UObject
{
    public string? AudioEventName;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        AudioEventName = GetOrDefault<FName>(nameof(AudioEventName)) is { IsNone: false } name ? name.Text : null;
    }
}
