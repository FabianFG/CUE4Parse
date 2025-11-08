using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Objects;

public class FGameplayEventFunction(FAssetArchive Ar) : FStructFallback(Ar, "GameplayEventFunction")
{
    public FSoftObjectPath Path = new FSoftObjectPath(Ar);
}
