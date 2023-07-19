using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UBlueprintCore : Assets.Exports.UObject
    {
        public FPackageIndex? SkeletonGeneratedClass;
        public FPackageIndex? GeneratedClass;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.BlueprintGeneratedClassIsAlwaysAuthoritative)
            {
                var _ = Ar.ReadBoolean(); // bLegacyGeneratedClassIsAuthoritative
            }

            if (Ar.Ver < EUnrealEngineObjectUE4Version.BLUEPRINT_SKEL_CLASS_TRANSIENT_AGAIN &&
                Ar.Ver != EUnrealEngineObjectUE4Version.BLUEPRINT_SKEL_TEMPORARY_TRANSIENT)
            {
                SkeletonGeneratedClass = new FPackageIndex(Ar);
                GeneratedClass = new FPackageIndex(Ar);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SkeletonGeneratedClass");
            serializer.Serialize(writer, SkeletonGeneratedClass);
        }
    }
}
