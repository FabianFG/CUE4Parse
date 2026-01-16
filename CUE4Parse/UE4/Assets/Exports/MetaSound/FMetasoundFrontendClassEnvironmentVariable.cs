using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassEnvironmentVariable
{
    public FName Name;
    public FName TypeName;
    public bool bIsRequired;

    public FMetasoundFrontendClassEnvironmentVariable(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        TypeName = fallback.GetOrDefault<FName>(nameof(TypeName));
        bIsRequired = fallback.GetOrDefault<bool>(nameof(bIsRequired));
    }
}