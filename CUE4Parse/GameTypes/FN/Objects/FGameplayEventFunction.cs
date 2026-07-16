using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.FN.Objects;

public class FGameplayEventFunction(FAssetArchive Ar) : FStructFallback(Ar, "GameplayEventFunction")
{
    public FStructFallback[] Functions = Ar.ReadArray(() => new FStructFallback(Ar, "GameplayEventHandlerFunctions"));
}

public class FGameplayEventDescriptor(FAssetArchive Ar) :  FStructFallback(Ar, "GameplayEventDescriptor")
{
    public FStructFallback[] Functions = Ar.ReadArray(() => new FStructFallback(Ar, "GameplayEventDefinition"));
}
