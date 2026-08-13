using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class FHavokAnyArray : FStructFallback
{
    public FScriptStruct?[]? Data;

    public FHavokAnyArray(FAssetArchive Ar, string structName) : base(Ar, structName)
    {
        var elementType = GetOrDefault<FPackageIndex>("ElementType");
        var numElements = GetOrDefault<int>("NumElements");
        Data = Ar.ReadArray(numElements, () => FScriptStruct.ReadInstancedStructWithoutSerialSize(Ar, elementType));
    }
}
