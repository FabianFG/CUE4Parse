using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UEnum : Assets.Exports.UObject
    {
        /** List of pairs of all enum names and values. */
        public (FName, long)[] Names;

        /** How the enum was originally defined. */
        public ECppForm CppForm;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Names = Ar.ReadArray(() => (Ar.ReadFName(), Ar.Read<long>()));
            CppForm = (ECppForm) Ar.Read<byte>();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Names");
            writer.WriteStartObject();
            {
                foreach (var (name, enumValue) in Names)
                {
                    writer.WritePropertyName(name.Text);
                    writer.WriteValue(enumValue);
                }
            }
            writer.WriteEndObject();

            writer.WritePropertyName("CppForm");
            writer.WriteValue(CppForm.ToString());
        }

        public enum ECppForm
        {
            Regular,
            Namespaced,
            EnumClass
        }
    }
}