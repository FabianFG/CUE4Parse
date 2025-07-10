using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkMediaAsset : UObject
{
    public FPackageIndex? CurrentMediaAssetData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        CurrentMediaAssetData = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (CurrentMediaAssetData is null) return;
        writer.WritePropertyName("CurrentMediaAssetData");
        serializer.Serialize(writer, CurrentMediaAssetData);
    }
}