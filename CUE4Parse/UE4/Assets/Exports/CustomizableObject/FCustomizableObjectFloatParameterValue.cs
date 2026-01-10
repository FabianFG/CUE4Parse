using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

[StructFallback]
public class FCustomizableObjectFloatParameterValue
{
    public string ParameterName;
    public float ParameterValue;
    public FGuid Id;
    public float[] ParameterRangeValues;
    
    public FCustomizableObjectFloatParameterValue(FStructFallback fallback)
    {
        ParameterName = fallback.GetOrDefault(nameof(ParameterName), string.Empty);
        ParameterValue = fallback.GetOrDefault<float>(nameof(ParameterValue));
        Id = fallback.GetOrDefault<FGuid>(nameof(Id));
        ParameterRangeValues = fallback.GetOrDefault<float[]>(nameof(ParameterRangeValues), []);
    }
}