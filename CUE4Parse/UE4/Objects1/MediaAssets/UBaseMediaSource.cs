using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.MediaAssets
{
    public class UBaseMediaSource : UMediaSource
    {
        public FName PlayerName;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            PlayerName = Ar.ReadFName();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (PlayerName.IsNone) return;
            writer.WritePropertyName("PlayerName");
            serializer.Serialize(writer, PlayerName);
        }
    }
}
