using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;

public class UFortPlayerFoleyWaveAssetContainer : UObject
{
    public FPackageIndex[] CookedWaveAssets;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        CookedWaveAssets = Ar.ReadArray(() => new FPackageIndex(Ar));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(CookedWaveAssets));
        serializer.Serialize(writer, CookedWaveAssets);
    }
}
