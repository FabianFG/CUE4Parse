using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

[StructFallback]
public class FCustomizableObjectIntParameterValue
{
    public string ParameterName;
    public string ParameterValueName;
    public FGuid Id;
    public string[] ParameterRangeValueNames;
    
    public FCustomizableObjectIntParameterValue(FStructFallback fallback)
    {
        ParameterName = fallback.GetOrDefault(nameof(ParameterName), string.Empty);
        ParameterValueName = fallback.GetOrDefault(nameof(ParameterValueName), string.Empty);
        Id = fallback.GetOrDefault<FGuid>(nameof(Id));
        ParameterRangeValueNames = fallback.GetOrDefault<string[]>(nameof(ParameterRangeValueNames), []);
    }
}