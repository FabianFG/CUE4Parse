using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendDocumentMetadata
{
    public FMetasoundFrontendVersion Version;
    
    public FMetasoundFrontendDocumentMetadata(FStructFallback fallback)
    {
        Version = fallback.GetOrDefault<FMetasoundFrontendVersion>(nameof(Version));
    }
}