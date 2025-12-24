using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.AoC.Objects;

[JsonConverter(typeof(FAoCFileConverter))]
public class FAoCFile : AbstractPropertyHolder
{
    public string Name;

    public FAoCFile(FAoCDBCReader Ar, string type)
    {
        Ar.Position = Ar.Position.Align(8);
        if (type is "CraftingStationVisualDefRecordBase" or "PathingRouteRecordBase" or "StoryArcPhaseRecordBase"
            or "SurveyPylonVisualDefRecordBase")
        {
            Ar.Position = Ar.Position.Align(16);
        }
        var props = new FStructFallback(Ar, type, new FRawHeader([(0, -3)], ERawHeaderFlags.RawProperties | ERawHeaderFlags.SuperStructs));
        if (props.GetOrDefault<long>("ParentGuid") != 0)
            Ar.Position += type == "ContributionThresholdsRecordBase" ? 2 : 4;
        var guid = props.GetOrDefault<long>("Guid", 0);
        var name = props.GetOrDefault<FName>("Name").Text;
        Name = name.EndsWith(guid.ToString()) ? name : name + "_" + guid;
        Properties.AddRange(props.Properties);
    }
}

public class FAoCFileConverter : JsonConverter<FAoCFile>
{
    public override FAoCFile? ReadJson(JsonReader reader, Type objectType, FAoCFile? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FAoCFile? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value.Properties.Count > 0)
        {
            writer.WritePropertyName(value.Name);
            writer.WriteStartObject();
            foreach (var property in value.Properties)
            {
                writer.WritePropertyName(property.ArrayIndex > 0 ? $"{property.Name.Text}[{property.ArrayIndex}]" : property.Name.Text);
                serializer.Serialize(writer, property.Tag);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
