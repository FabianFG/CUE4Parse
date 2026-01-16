using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendVersion
{
    public FName Name;
    public FMetasoundFrontendVersionNumber Number;
    
    public FMetasoundFrontendVersion(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        Number = fallback.GetOrDefault<FMetasoundFrontendVersionNumber>(nameof(Number));
    }
}