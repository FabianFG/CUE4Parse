using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendNodeInterface
{
    public FMetasoundFrontendVertex[] Inputs;
    public FMetasoundFrontendVertex[] Outputs;
    public FMetasoundFrontendVertex[] Environment;
    
    public FMetasoundFrontendNodeInterface(FStructFallback fallback)
    {
        Inputs = fallback.GetOrDefault<FMetasoundFrontendVertex[]>(nameof(Inputs), []);
        Outputs = fallback.GetOrDefault<FMetasoundFrontendVertex[]>(nameof(Outputs), []);
        Environment = fallback.GetOrDefault<FMetasoundFrontendVertex[]>(nameof(Environment), []);
    }
}