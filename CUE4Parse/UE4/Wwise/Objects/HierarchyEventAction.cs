using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyEventAction : AbstractHierarchy
    {
        public readonly EEventActionScope EventActionScope;
        public readonly EEventActionType EventActionType;
        public readonly uint ReferencedId;
        public readonly short ParameterCount;

        public HierarchyEventAction(FArchive Ar) : base(Ar)
        {
            EventActionScope = Ar.Read<EEventActionScope>();
            EventActionType = Ar.Read<EEventActionType>();

            ReferencedId = Ar.Read<uint>();
            var reversedCount = Ar.ReadArray<byte>(sizeof(short));
            Array.Reverse(reversedCount);
            ParameterCount = BitConverter.ToInt16(reversedCount, 0);

            // TODO: https://web.archive.org/web/20230818023606/http://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#type_.233:_Event_Action
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("EventActionScope");
            writer.WriteValue(EventActionScope.ToString());

            writer.WritePropertyName("EventActionType");
            writer.WriteValue(EventActionType.ToString());

            if (ReferencedId != 0)
            {
                writer.WritePropertyName("ReferencedId");
                writer.WriteValue(ReferencedId);
            }

            writer.WritePropertyName("ParameterCount");
            writer.WriteValue(ParameterCount);

            writer.WriteEndObject();
        }
    }
}
