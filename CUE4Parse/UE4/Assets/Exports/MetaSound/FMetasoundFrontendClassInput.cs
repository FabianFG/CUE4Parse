using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassInput : FMetasoundFrontendClassVertex
{
    public FMetasoundFrontendClassInputDefault[] Defaults;

    public FMetasoundFrontendClassInput(FStructFallback fallback) : base(fallback)
    {
        Defaults = fallback.GetOrDefault<FMetasoundFrontendClassInputDefault[]>(nameof(Defaults), []);
    }
}