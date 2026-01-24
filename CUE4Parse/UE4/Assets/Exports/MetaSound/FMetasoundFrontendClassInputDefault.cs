using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassInputDefault
{
    public FMetasoundFrontendLiteral Literal;
    public FGuid PageID;
    
    public FMetasoundFrontendClassInputDefault(FStructFallback fallback)
    {
        Literal = fallback.GetOrDefault<FMetasoundFrontendLiteral>(nameof(Literal));
        PageID = fallback.GetOrDefault<FGuid>(nameof(PageID));
    }
}