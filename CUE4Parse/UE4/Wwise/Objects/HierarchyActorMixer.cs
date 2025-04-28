using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyActorMixer : AbstractHierarchy
    {
        public readonly uint DirectParentID;

        public HierarchyActorMixer(FArchive Ar) : base(Ar)
        {
            Ar.Read<byte>(); // bIsOverrideParentFX
            Ar.Read<byte>(); // uNumFx (parent FX count)
            Ar.Read<byte>(); // bIsOverrideParentMetadata
            Ar.Read<byte>(); // uNumFx (metadata FX count)

            Ar.Read<uint>(); // Read OverrideBusId

            DirectParentID = Ar.Read<uint>();
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("DirectParentID");
            writer.WriteValue(DirectParentID);

            writer.WriteEndObject();
        }
    }
}

