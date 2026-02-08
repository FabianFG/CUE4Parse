using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkMediaAsset : UObject
{
    public uint ID { get; private set; }
    public string MediaName { get; private set; } = string.Empty;
    public FPackageIndex? CurrentMediaAssetData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ID = GetOrDefault<uint>(nameof(ID));
        MediaName = GetOrDefault<string>(nameof(MediaName));
        CurrentMediaAssetData = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (CurrentMediaAssetData is null) return;

        writer.WritePropertyName(nameof(CurrentMediaAssetData));
        serializer.Serialize(writer, CurrentMediaAssetData);
    }
}
