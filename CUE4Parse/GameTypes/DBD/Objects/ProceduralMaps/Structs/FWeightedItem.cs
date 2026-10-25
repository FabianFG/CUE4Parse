using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FWeightedItem
{
    public readonly FSoftObjectPath Element;
    public readonly float Weight;

    public FWeightedItem(FSoftObjectPath element, float weight)
    {
        Element = element;
        Weight = weight;
    }

    public FWeightedItem(FStructFallback fallback)
    {
        Element = fallback.GetOrDefault<FSoftObjectPath>(nameof(Element));
        Weight = fallback.GetOrDefault(nameof(Weight), 1f);
    }
}
