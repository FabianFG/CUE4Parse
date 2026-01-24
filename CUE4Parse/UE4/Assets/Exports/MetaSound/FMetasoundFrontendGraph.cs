using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendGraph
{
    public FMetasoundFrontendNode[] Nodes;
    public FMetasoundFrontendEdge[] Edges;
    public FMetasoundFrontendVariable[] Variables;
    public FGuid PageID;
    
    public FMetasoundFrontendGraph(FStructFallback fallback)
    {
        Nodes = fallback.GetOrDefault<FMetasoundFrontendNode[]>(nameof(Nodes), []);
        Edges = fallback.GetOrDefault<FMetasoundFrontendEdge[]>(nameof(Edges), []);
        Variables = fallback.GetOrDefault<FMetasoundFrontendVariable[]>(nameof(Variables), []);
        PageID = fallback.GetOrDefault<FGuid>(nameof(PageID));
    }
}