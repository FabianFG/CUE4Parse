using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Particles
{
    public class UParticleModuleRequired : Assets.Exports.UObject
    {
        // FSubUVDerivedData
        public FVector2D[]? BoundingGeometry;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MovedParticleCutoutsToRequiredModule) return;
            var bCooked = Ar.ReadBoolean();

            if (bCooked)
            {
                BoundingGeometry = Ar.ReadArray<FVector2D>();
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (BoundingGeometry is not { Length: > 0 }) return;
            writer.WritePropertyName("BoundingGeometry");
            serializer.Serialize(writer, BoundingGeometry);
        }
    }
}
