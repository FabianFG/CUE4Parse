using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassName
{
    public FName Namespace;
    public FName Name;
    public FName Variant;
    
    public FMetasoundFrontendClassName(FStructFallback fallback)
    {
        Namespace = fallback.GetOrDefault<FName>(nameof(Namespace));
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        Variant = fallback.GetOrDefault<FName>(nameof(Variant));
    }
}