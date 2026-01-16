using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassOutput : FMetasoundFrontendClassVertex
{
    public FMetasoundFrontendClassOutput(FStructFallback fallback) : base(fallback) { }
}