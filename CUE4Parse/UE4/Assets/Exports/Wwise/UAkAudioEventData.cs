using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioEventData : UAkAssetDataSwitchContainer
{
    public ResolvedObject[] MediaList { get; private set; } = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        MediaList = GetOrDefault<ResolvedObject[]>(nameof(MediaList)) ?? [];
    }
}
