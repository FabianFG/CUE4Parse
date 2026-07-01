using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UField : Assets.Exports.UObject
    {
        public FPackageIndex? SuperField;

        /** Next Field in the linked list */
        public FPackageIndex? Next; // UField

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Ver < EUnrealEngineObjectUE3Version.MOVED_SUPERFIELD_TO_USTRUCT)
            {
                SuperField = new FPackageIndex(Ar);
            }

            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.RemoveUField_Next)
            {
                Next = new FPackageIndex(Ar);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (Next is { IsNull: false })
            {
                writer.WritePropertyName("Next");
                serializer.Serialize(writer, Next);
            }
        }
    }
}
