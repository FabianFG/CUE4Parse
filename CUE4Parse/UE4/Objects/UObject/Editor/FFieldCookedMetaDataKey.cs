using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FFieldCookedMetaDataKey : IUStruct
{
    public FName[] FieldPath;

    public FFieldCookedMetaDataKey(FStructFallback fallback)
    {
        FieldPath = fallback.GetOrDefault<FName[]>(nameof(FieldPath), []);
    }
}
