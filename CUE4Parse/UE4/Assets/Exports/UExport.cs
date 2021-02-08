using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports
{
    public abstract class UExport
    {
        public string ExportType { get; protected set; }
        public FObjectExport? Export { get; }
        public string Name { get; protected set; }
        public IPackage? Owner { get; set; }
        
        public abstract void Deserialize(FAssetArchive Ar, long validPos);

        protected UExport(string exportType)
        {
            ExportType = exportType;
            Name = exportType;
        }

        protected UExport(FObjectExport exportObject) : this(exportObject.ClassName)
        {
            Export = exportObject;
            Name = exportObject.ObjectName.Text;
        }

        public override string ToString() => $"{Name} | {ExportType}";
    }

    public class UExportConverter : JsonConverter<UExport>
    {
        public override void WriteJson(JsonWriter writer, UExport value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            writer.WritePropertyName("Export");
            switch (value)
            {
                case UObject o:
                    break;
                default:
                    writer.WriteNull();
                    break;
            }
            
            writer.WriteEndObject();
        }

        public override UExport ReadJson(JsonReader reader, Type objectType, UExport existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}