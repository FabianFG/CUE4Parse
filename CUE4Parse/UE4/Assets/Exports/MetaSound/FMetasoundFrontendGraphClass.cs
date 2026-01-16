using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendGraphClass : FMetasoundFrontendClass
{
    public FMetasoundFrontendGraph[] PagedGraphs;
    
    public FMetasoundFrontendGraphClass(FStructFallback fallback) : base(fallback)
    {
        PagedGraphs = fallback.GetOrDefault<FMetasoundFrontendGraph[]>(nameof(PagedGraphs), []);
    }
}