using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public class FVectorParameterValue
    {
        public string Name => ParameterName?.Text ?? ParameterInfo.Name.Text;
        public readonly FName? ParameterName;
        public readonly FLinearColor? ParameterValue;
        public readonly FMaterialParameterInfo ParameterInfo;

        public FVectorParameterValue(FStructFallback fallback)
        {
            ParameterName = fallback.GetOrDefault<FName>(nameof(ParameterName));
            ParameterValue = fallback.GetOrDefault<FLinearColor>(nameof(ParameterValue));
            ParameterInfo = fallback.GetOrDefault<FMaterialParameterInfo>(nameof(ParameterInfo));
        }

        public override string ToString() => $"{Name}: {ParameterValue}";
    }
}