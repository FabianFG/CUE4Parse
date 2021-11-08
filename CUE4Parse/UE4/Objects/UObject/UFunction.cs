using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UFunction : UStruct
    {
        public uint FunctionFlags;
        public FPackageIndex EventGraphFunction; // UFunction
        public int EventGraphCallOffset;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            FunctionFlags = Ar.Read<uint>();

            // Replication info
            if ((FunctionFlags & 0x40 /*EFunctionFlags.FUNC_Net*/) != 0)
            {
                // Unused.
                var repOffset = Ar.Read<short>();
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_BLUEPRINT_EVENTGRAPH_FASTCALLS_IN_UFUNCTION)
            {
                EventGraphFunction = new FPackageIndex(Ar);
                EventGraphCallOffset = Ar.Read<int>();
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("FunctionFlags");
            writer.WriteValue(FunctionFlags);

            if (EventGraphFunction is { IsNull: false })
            {
                writer.WritePropertyName("EventGraphFunction");
                serializer.Serialize(writer, EventGraphFunction);
            }

            if (EventGraphCallOffset != 0)
            {
                writer.WritePropertyName("EventGraphCallOffset");
                writer.WriteValue(EventGraphCallOffset);
            }
        }
    }
}