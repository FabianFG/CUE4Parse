using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework.Shader;

public class FComputeKernelShaderMapContent : FShaderMapContent
{
    public string FriendlyName;
    public FComputeKernelShaderMapId ShaderMapId;
    
    public FComputeKernelShaderMapContent(FMemoryImageArchive Ar) : base(Ar)
    {
        FriendlyName = Ar.ReadFString();
        ShaderMapId = new FComputeKernelShaderMapId(Ar);
    }
}