using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework.Shader;

public class FComputeKernelShaderMapContent : FShaderMapContent
{
    public string FriendlyName;
    public FComputeKernelShaderMapId ShaderMapId;

    public override void Deserialize(FMemoryImageArchive Ar)
    {
        base.Deserialize(Ar);
        
        FriendlyName = Ar.ReadFString();
        ShaderMapId = new FComputeKernelShaderMapId(Ar);
    }
}