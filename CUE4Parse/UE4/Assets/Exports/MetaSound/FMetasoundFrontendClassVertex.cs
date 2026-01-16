using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

public class FMetasoundFrontendClassVertex : FMetasoundFrontendVertex
{
    public FGuid NodeID;
    public EMetasoundFrontendVertexAccessType AccessType;
    
    public FMetasoundFrontendClassVertex(FStructFallback fallback) : base(fallback)
    {
        NodeID = fallback.GetOrDefault<FGuid>(nameof(NodeID));
        AccessType = fallback.GetOrDefault<EMetasoundFrontendVertexAccessType>(nameof(AccessType));
    }
}

public enum EMetasoundFrontendVertexAccessType
{
    Reference,	//< The vertex accesses data by reference.
    Value,		//< The vertex accesses data by value.

    Unset		//< The vertex access level is unset (ex. vertex on an unconnected reroute node).
    //< Not reflected as a graph core access type as core does not deal with reroutes
    //< or ambiguous accessor level (it is resolved during document pre-processing).
}