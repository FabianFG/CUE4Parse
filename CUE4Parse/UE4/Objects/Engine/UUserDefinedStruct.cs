using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public enum EUserDefinedStructureStatus
    {
        /** Struct is in an unknown state. */
        UDSS_UpToDate,

        /** Struct has been modified but not recompiled. */
        UDSS_Dirty,

        /** Struct tried but failed to be compiled. */
        UDSS_Error,

        /** Struct is a duplicate, the original one was changed. */
        UDSS_Duplicate
    }

    public class UUserDefinedStruct : UStruct
    {
        public EUserDefinedStructureStatus Status;
        public uint StructFlags;
        public List<FPropertyTag>? DefaultProperties { get; set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Status = GetOrDefault<EUserDefinedStructureStatus>(nameof(Status));
            if (Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject)) return;
            if (Status != EUserDefinedStructureStatus.UDSS_UpToDate) return;

            StructFlags = Ar.Read<uint>();

            if (Ar.HasUnversionedProperties)
            {
                DeserializePropertiesUnversioned(DefaultProperties = new List<FPropertyTag>(), Ar, this);
            }
            else
            {
                DeserializePropertiesTagged(DefaultProperties = new List<FPropertyTag>(), Ar, true);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);
            writer.WritePropertyName("StructFlags");
            writer.WriteValue(StructFlags);

            if (DefaultProperties is not { Count: > 0 })
                return;
            writer.WritePropertyName("DefaultProperties");
            writer.WriteStartObject();
            foreach (var property in DefaultProperties)
            {
                writer.WritePropertyName(property.Name.Text);
                serializer.Serialize(writer, property.Tag);
            }
            writer.WriteEndObject();

        }
    }
}
