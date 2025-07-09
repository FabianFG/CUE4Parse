using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class UPoseSearchDatabase : UDataAsset
{
    public FSearchIndex SearchIndexPrivate;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SearchIndexPrivate = new FSearchIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(SearchIndexPrivate));
        serializer.Serialize(writer, SearchIndexPrivate);
    }
}
