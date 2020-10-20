using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Materials
{
    [StructFallback]
    public class FMaterialParameterInfo
    {
        public readonly FName Name;

        public FMaterialParameterInfo(FStructFallback fallback)
        {
            Name = fallback.GetOrDefault<FName>(nameof(Name));
        }
    }
}