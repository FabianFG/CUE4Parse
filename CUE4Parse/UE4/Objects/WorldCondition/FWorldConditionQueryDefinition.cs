using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.WorldCondition;

[JsonConverter(typeof(FWorldConditionQueryDefinitionConverter))]
public class FWorldConditionQueryDefinition : IUStruct
{
    public FStructFallback StaticStruct;

    public FWorldConditionQueryDefinition(FAssetArchive Ar)
    {
        StaticStruct = new FStructFallback(Ar, "WorldConditionQueryDefinition");

        if (FWorldConditionCustomVersion.Get(Ar) >= FWorldConditionCustomVersion.Type.StructSharedDefinition)
        {
            var bHasSharedDefinition = Ar.ReadBoolean();
        }
    }
}

public class FWorldConditionQueryDefinitionConverter : JsonConverter<FWorldConditionQueryDefinition>
{
    public override void WriteJson(JsonWriter writer, FWorldConditionQueryDefinition value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.StaticStruct);
    }

    public override FWorldConditionQueryDefinition ReadJson(JsonReader reader, Type objectType, FWorldConditionQueryDefinition existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
