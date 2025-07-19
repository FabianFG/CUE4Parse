using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class FHavokAIAnyArray : FStructFallback
{
    public FStructFallback[]? Data;

    public FHavokAIAnyArray(FAssetArchive Ar) : base(Ar, "HavokAIAnyArray")
    {
        var elementType = GetOrDefault<FPackageIndex>("ElementType");
        var numElements = GetOrDefault<int>("NumElements");
        if (numElements == 0)
        {
            Data = [];
        }
        else if (elementType.TryLoad<UStruct>(out var struc))
        {
            Data = Ar.ReadArray(numElements, () => new FStructFallback(Ar, struc));
        }
        else if (elementType.ResolvedObject is { } obj)
        {
            Data = Ar.ReadArray(numElements, () => new FStructFallback(Ar, obj.Name.ToString()));
        }
        else
        {
            throw new ParserException($"Failed to load ElementType : {elementType} for FHavokAIAnyArray");
        }
    }
}
