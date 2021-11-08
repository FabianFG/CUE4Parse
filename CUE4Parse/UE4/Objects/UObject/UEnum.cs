using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [SkipObjectRegistration]
    public class UEnum : Assets.Exports.UObject
    {
        /** List of pairs of all enum names and values. */
        public (FName, long)[] Names;

        /** How the enum was originally defined. */
        public ECppForm CppForm;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            if (Ar.Ver < EUnrealEngineObjectUE4Version.TIGHTLY_PACKED_ENUMS)
            {
                var tempNames = Ar.ReadArray(Ar.ReadFName);
                Names = new (FName, long)[tempNames.Length];
                for (var value = 0; value < tempNames.Length; value++)
                {
                    Names[value] = (tempNames[value], value);
                }
            }
            else if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.EnumProperties)
            {
                var oldNames = Ar.ReadArray(() => (Ar.ReadFName(), Ar.Read<byte>()));
                Names = new (FName, long)[oldNames.Length];
                for (var value = 0; value < oldNames.Length; value++)
                {
                    Names[value] = oldNames[value];
                }
            }
            else
            {
                Names = Ar.ReadArray(() => (Ar.ReadFName(), Ar.Read<long>()));
            }

            if (Ar.Ver < EUnrealEngineObjectUE4Version.ENUM_CLASS_SUPPORT)
            {
                var bIsNamespace = Ar.ReadBoolean();
                CppForm = bIsNamespace ? ECppForm.Namespaced : ECppForm.Regular;
            }
            else
            {
                CppForm = Ar.Read<ECppForm>();
            }
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

        public enum ECppForm : byte
        {
            Regular,
            Namespaced,
            EnumClass
        }
    }
}