using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public class UMaterialExpressionQualitySwitch : UMaterialExpression
{
    public readonly FExpressionInput[] Inputs = new FExpressionInput[(int) EMaterialQualityLevel.Num];
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var toRemove = new List<FPropertyTag>();

        var i = 0;
        foreach (var property in Properties)
        {
            if (property.Tag?.GenericValue is FScriptStruct { StructType: FExpressionInput input } && property.Name.Text == "Inputs")
            {
                Inputs[i] = input;
                toRemove.Add(property);
                i++;
            }
        }

        foreach (var remove in toRemove)
        {
            Properties.Remove(remove);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Inputs");
        writer.WriteStartArray();

        foreach (var input in Inputs)
        {
            serializer.Serialize(writer, input);
        }

        writer.WriteEndArray();
    }
}
