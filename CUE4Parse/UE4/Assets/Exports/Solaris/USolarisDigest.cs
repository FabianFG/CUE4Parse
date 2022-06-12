using System.Text;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Solaris
{
    /// <summary>
    /// ULang Visual Process Language
    /// </summary>
    public class USolarisDigest : Assets.Exports.UObject
    {
        public string ProjectName { get; private set; }
        public string ReadableCode { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            ProjectName = GetOrDefault<string>(nameof(ProjectName));

            ReadableCode = Encoding.UTF8.GetString(GetOrDefault<byte[]>("DigestCode"));
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("ReadableCode");
            writer.WriteValue(ReadableCode);
        }
    }
}
