using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.RigVM
{
    public class URigVM : Assets.Exports.UObject
    {
        public FRigVMMemoryContainer WorkMemoryStorage;
        public FRigVMMemoryContainer LiteralMemoryStorage;
        public FRigVMByteCode ByteCodeStorage;
        public FRigVMParameter[] Parameters;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            // if (FAnimObjectVersion.Get(Ar) < FAnimObjectVersion.Type.StoreMarkerNamesOnSkeleton) return;
            // WorkMemoryStorage = new FRigVMMemoryContainer(Ar);
            // LiteralMemoryStorage = new FRigVMMemoryContainer(Ar);
            // ByteCodeStorage = new FRigVMByteCode(Ar);
            // Parameters = Ar.ReadArray(() => new FRigVMParameter(Ar));

            Ar.Position = validPos;
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("WorkMemoryStorage");
            serializer.Serialize(writer, WorkMemoryStorage);

            writer.WritePropertyName("LiteralMemoryStorage");
            serializer.Serialize(writer, LiteralMemoryStorage);

            writer.WritePropertyName("ByteCodeStorage");
            serializer.Serialize(writer, ByteCodeStorage);

            writer.WritePropertyName("Parameters");
            serializer.Serialize(writer, Parameters);
        }
    }
}
