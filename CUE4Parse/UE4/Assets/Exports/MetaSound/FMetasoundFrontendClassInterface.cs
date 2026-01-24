using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendClassInterface
{
    public FMetasoundFrontendClassInput[] Inputs;
    public FMetasoundFrontendClassOutput[] Outputs;
    public FMetasoundFrontendClassEnvironmentVariable[] Environment;
    
    public FMetasoundFrontendClassInterface(FStructFallback fallback)
    {
        Inputs = fallback.GetOrDefault<FMetasoundFrontendClassInput[]>(nameof(Inputs), []);
        Outputs = fallback.GetOrDefault<FMetasoundFrontendClassOutput[]>(nameof(Outputs), []);
        Environment = fallback.GetOrDefault<FMetasoundFrontendClassEnvironmentVariable[]>(nameof(Environment), []);
    }
}