using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework.Shader;

public class FComputeKernelShaderMap : FShaderMapBase
{
    protected override FShaderMapContent ReadContent(FMemoryImageArchive Ar) => new FComputeKernelShaderMapContent(Ar);
}