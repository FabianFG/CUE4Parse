using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendVersionNumber
{
    public int Major;
    public int Minor;
    
    public FMetasoundFrontendVersionNumber()
    {
        Major = Minor = 0;
    }

    public FMetasoundFrontendVersionNumber(FStructFallback fallback)
    {
        Major = fallback.GetOrDefault<int>(nameof(Major));
        Minor = fallback.GetOrDefault<int>(nameof(Minor));
    }
}