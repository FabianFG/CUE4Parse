using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendVariable
{
    public FName Name;
    public FName TypeName;
    public FMetasoundFrontendLiteral Literal;
    public FGuid ID;
    public FGuid VariableNodeID;
    public FGuid MutatorNodeID;
    public FGuid[] AccessorNodeIDs;
    public FGuid[] DeferredAccessorNodeIDs;
    
    public FMetasoundFrontendVariable(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        TypeName = fallback.GetOrDefault<FName>(nameof(TypeName));
        Literal = fallback.GetOrDefault<FMetasoundFrontendLiteral>(nameof(Literal));
        ID = fallback.GetOrDefault<FGuid>(nameof(ID));
        VariableNodeID = fallback.GetOrDefault<FGuid>(nameof(VariableNodeID));
        MutatorNodeID = fallback.GetOrDefault<FGuid>(nameof(MutatorNodeID));
        AccessorNodeIDs = fallback.GetOrDefault<FGuid[]>(nameof(AccessorNodeIDs), []);
        DeferredAccessorNodeIDs = fallback.GetOrDefault<FGuid[]>(nameof(DeferredAccessorNodeIDs), []);
    }
}