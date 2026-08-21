using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.WorldCondition;

[JsonConverter(typeof(FWorldConditionQueryDefinitionConverter))]
public class FWorldConditionQueryDefinition : IUStruct
{
    public FStructFallback StaticStruct;
    public FStructFallback SharedDefinition;

    public FWorldConditionQueryDefinition(FAssetArchive Ar)
    {
        StaticStruct = new FStructFallback(Ar, Ar.ResolveTypeIdentifier("WorldConditionQueryDefinition"));

        if (FWorldConditionCustomVersion.Get(Ar) >= FWorldConditionCustomVersion.Type.StructSharedDefinition)
        {
            var bHasSharedDefinition = Ar.ReadBoolean();
            if (bHasSharedDefinition)
                SharedDefinition = new FStructFallback(Ar,
                    Ar.ResolveTypeIdentifier("WorldConditionQuerySharedDefinition"));
        }
    }
}
