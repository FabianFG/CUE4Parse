using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendVertex
{
    public FName Name;
    public FName TypeName;
    public FGuid VertexID;
    
    public FMetasoundFrontendVertex(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        TypeName = fallback.GetOrDefault<FName>(nameof(TypeName));
        VertexID = fallback.GetOrDefault<FGuid>(nameof(VertexID));
    }
}