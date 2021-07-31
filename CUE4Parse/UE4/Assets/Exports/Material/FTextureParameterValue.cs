using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public class FTextureParameterValue
    {
        public string Name => ParameterName?.Text ?? ParameterInfo.Name.Text;
        public readonly FName? ParameterName;
        public readonly FPackageIndex ParameterValue; // UTexture
        public readonly FMaterialParameterInfo ParameterInfo;

        public FTextureParameterValue(FStructFallback fallback)
        {
            ParameterName = fallback.GetOrDefault<FName>(nameof(ParameterName));
            ParameterValue = fallback.GetOrDefault<FPackageIndex>(nameof(ParameterValue));
            ParameterInfo = fallback.GetOrDefault<FMaterialParameterInfo>(nameof(ParameterInfo));
        }
        
        public override string ToString() => $"{Name}: {ParameterValue}";
    }
}