using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchySoundSfxVoice : AbstractHierarchy
    {
        public readonly ESoundConversion SoundConversion;
        public readonly ESoundSource SoundSource;
        public readonly ESoundType SoundType;
        public readonly uint SoundId;
        public readonly uint SourceId;
        public readonly uint? WemOffset;
        public readonly uint? WemLength;
        public readonly SoundStructure SoundStructureData;



        public HierarchySoundSfxVoice(FArchive Ar) : base(Ar)
        {
            SoundConversion = Ar.Read<ESoundConversion>();
            //Ar.Position += 4;
            SoundSource = Ar.Read<ESoundSource>();
            SoundId = Ar.Read<uint>();
            SourceId = Ar.Read<uint>();

            if (SoundSource == ESoundSource.Embedded)
            {
                WemOffset = Ar.Read<uint>();
                WemLength = Ar.Read<uint>();
            }

            SoundType = Ar.Read<ESoundType>();
            SoundStructureData = new SoundStructure(Ar);

            // TODO: https://web.archive.org/web/20230818023606/http://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#Sound_structure
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("SoundConversion");
            writer.WriteValue(SoundConversion.ToString());

            writer.WritePropertyName("SoundSource");
            writer.WriteValue(SoundSource.ToString());

            writer.WritePropertyName("SoundId");
            writer.WriteValue(SoundId);

            writer.WritePropertyName("SourceId");
            writer.WriteValue(SourceId);

            if (SoundSource == ESoundSource.Embedded)
            {
                writer.WritePropertyName("EmbeddedWem");

                writer.WriteStartObject();

                writer.WritePropertyName("WemLength");
                writer.WriteValue(WemLength);

                writer.WritePropertyName("WemOffset");
                writer.WriteValue(WemOffset);

                writer.WriteEndObject();
            }

            writer.WritePropertyName("SoundType");
            writer.WriteValue(SoundType.ToString());

            writer.WritePropertyName("SoundStructure");
            SoundStructureData.WriteJson(writer, serializer);

            writer.WriteEndObject();
        }
    }
}
