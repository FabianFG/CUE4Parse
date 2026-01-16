using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendEdge
{
    public FGuid FromNodeID;
    public FGuid FromVertexID;
    public FGuid ToNodeID;
    public FGuid ToVertexID;
    
    public FMetasoundFrontendEdge(FStructFallback fallback)
    {
        FromNodeID = fallback.GetOrDefault<FGuid>(nameof(FromNodeID));
        FromVertexID = fallback.GetOrDefault<FGuid>(nameof(FromVertexID));
        ToNodeID = fallback.GetOrDefault<FGuid>(nameof(ToNodeID));
        ToVertexID = fallback.GetOrDefault<FGuid>(nameof(ToVertexID));
    }
}