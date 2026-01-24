using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendDocument
{
    public FMetasoundFrontendDocumentMetadata Metadata;
    public FMetasoundFrontendVersion[] Interfaces;
    public FMetasoundFrontendGraphClass RootGraph;
    public FMetasoundFrontendGraphClass[] Subgraphs;
    public FMetasoundFrontendClass[] Dependencies;
    
    public FMetasoundFrontendDocument(FStructFallback fallback)
    {
        Metadata = fallback.GetOrDefault<FMetasoundFrontendDocumentMetadata>(nameof(Metadata));
        Interfaces = fallback.GetOrDefault<FMetasoundFrontendVersion[]>(nameof(Interfaces), []);
        RootGraph = fallback.GetOrDefault<FMetasoundFrontendGraphClass>(nameof(RootGraph));
        Subgraphs = fallback.GetOrDefault<FMetasoundFrontendGraphClass[]>(nameof(Subgraphs));
        Dependencies = fallback.GetOrDefault<FMetasoundFrontendClass[]>(nameof(Dependencies));
    }
}