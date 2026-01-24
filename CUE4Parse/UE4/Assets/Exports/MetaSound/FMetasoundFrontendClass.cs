using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClass
{
    public FGuid ID;
    public FMetasoundFrontendClassMetadata Metadata;
    public FMetasoundFrontendClassInterface Interface;
    
    public FMetasoundFrontendClass(FStructFallback fallback)
    {
        ID = fallback.GetOrDefault<FGuid>(nameof(ID));
        Metadata = fallback.GetOrDefault<FMetasoundFrontendClassMetadata>(nameof(Metadata));
        Interface = fallback.GetOrDefault<FMetasoundFrontendClassInterface>(nameof(Interface));
    }
}