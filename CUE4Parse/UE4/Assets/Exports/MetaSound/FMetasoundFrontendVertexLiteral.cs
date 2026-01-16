using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendVertexLiteral
{
    public FGuid VertexID;
    public FMetasoundFrontendLiteral Value;
    
    public FMetasoundFrontendVertexLiteral(FStructFallback fallback)
    {
        VertexID = fallback.GetOrDefault<FGuid>(nameof(VertexID));
        Value = fallback.GetOrDefault<FMetasoundFrontendLiteral>(nameof(Value));
    }
}