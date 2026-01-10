using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

[StructFallback]
public class FCustomizableObjectInstanceDescriptor
{
    public FPackageIndex CustomizableObject;
    public FCustomizableObjectIntParameterValue[] IntParameters;
    public FCustomizableObjectFloatParameterValue[] FloatParameters;
    
    public FCustomizableObjectInstanceDescriptor(FStructFallback fallback)
    {
        CustomizableObject = fallback.GetOrDefault(nameof(CustomizableObject), new FPackageIndex());
        IntParameters = fallback.GetOrDefault<FCustomizableObjectIntParameterValue[]>(nameof(IntParameters), []);
        FloatParameters = fallback.GetOrDefault<FCustomizableObjectFloatParameterValue[]>(nameof(FloatParameters), []);
    }
}