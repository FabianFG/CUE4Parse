using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine
{
    [JsonConverter(typeof(UCurveTableConverter))]
    public class UCurveTable : UObject
    {
        public Dictionary<FName, UObject> RowMap { get; private set; } // UObject is actually FRealCurve aka FSimpleCurve if CurveTableMode is SimpleCurves else FRichCurve
        public ECurveTableMode CurveTableMode { get; private set; }

        public UCurveTable() { }
        public UCurveTable(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var numRows = Ar.Read<int>();
            CurveTableMode = Ar.Read<ECurveTableMode>();
            RowMap = new Dictionary<FName, UObject>(numRows);
            for(var i = 0; i < numRows; i++)
            {
                var rowName = Ar.ReadFName();
                string exportType = CurveTableMode switch
                {
                    ECurveTableMode.SimpleCurves => "SimpleCurve",
                    ECurveTableMode.RichCurves => "RichCurve",
                    _ => ""
                };
                UObject rowValue = new UObject(new List<FPropertyTag>(), null, exportType);
                rowValue.Deserialize(Ar, -1);
                RowMap[rowName] = rowValue;
            }
        }
    }
    
    public class UCurveTableConverter : JsonConverter<UCurveTable>
    {
        public override void WriteJson(JsonWriter writer, UCurveTable value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            // export properties
            writer.WritePropertyName("Export");
            writer.WriteStartObject();
            {
                foreach (var kvp in value.RowMap)
                {
                    writer.WritePropertyName(kvp.Key.Text);
                    serializer.Serialize(writer, kvp.Value); // will write CurveTableMode
                }
            }
            writer.WriteEndObject();
            
            writer.WriteEndObject();
        }

        public override UCurveTable ReadJson(JsonReader reader, Type objectType, UCurveTable existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
