using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioEventData : UAkAssetDataSwitchContainer
{
    public FPackageIndex[] MediaList { get; private set; } = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        MediaList = GetOrDefault<FPackageIndex[]>(nameof(MediaList)) ?? [];
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
    }
}
