using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font
{
    public class UFontFace : UObject
    {
        public FFontFaceData? FontFaceData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var bSaveInlineData = Ar.ReadBoolean();
            if (bSaveInlineData)
            {
                FontFaceData = new FFontFaceData(Ar);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (FontFaceData == null) return;
            // writer.WritePropertyName("FontFaceData");
            // serializer.Serialize(writer, FontFaceData);
        }
    }

    public class FFontFaceData
    {
        public byte[] Data;

        public FFontFaceData(FArchive Ar)
        {
            Data = Ar.ReadArray<byte>();
        }
    }
}
