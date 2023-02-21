using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh
{
    public class UStaticMeshComponent : UObject
    {
        public FStaticMeshComponentLODInfo[]? LODData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            LODData = Ar.ReadArray(() => new FStaticMeshComponentLODInfo(Ar));
        }

        public FPackageIndex GetStaticMesh()
        {
            var mesh = new FPackageIndex();
            var current = this;
            while (true)
            {
                mesh = current.GetOrDefault("StaticMesh", new FPackageIndex());
                if (!mesh.IsNull || current.Template == null)
                    break;
                current = current.Template.Load<UStaticMeshComponent>();
            }

            return mesh;
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (LODData is { Length: <= 0 }) return;
            writer.WritePropertyName("LODData");
            serializer.Serialize(writer, LODData);
        }
    }
}
