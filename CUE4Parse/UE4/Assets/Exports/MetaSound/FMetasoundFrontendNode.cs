using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendNode
{
    public FGuid ID;
    public FGuid ClassID;
    public FName Name;
    public FMetasoundFrontendNodeInterface Interface;
    public FMetasoundFrontendVertexLiteral[] InputLiterals;
    
    public FMetasoundFrontendNode(FStructFallback fallback)
    {
        ID = fallback.GetOrDefault<FGuid>(nameof(ID));
        ClassID = fallback.GetOrDefault<FGuid>(nameof(ClassID));
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        Interface = fallback.GetOrDefault<FMetasoundFrontendNodeInterface>(nameof(Interface));
        InputLiterals = fallback.GetOrDefault<FMetasoundFrontendVertexLiteral[]>(nameof(InputLiterals), []);
    }
}