using CUE4Parse.UE4.Assets.Exports.ComputerFramework.Shader;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework;

public class FComputeKernelResource
{
    public FComputeKernelShaderMap? GameThreadShaderMap;
    
    public FComputeKernelResource(FArchive Ar)
    {
        var bCooked = Ar.ReadBoolean();

        if (bCooked)
        {
            var bValid = Ar.ReadBoolean();

            if (bValid)
            {
                GameThreadShaderMap = new FComputeKernelShaderMap();
                GameThreadShaderMap.Deserialize(new FMaterialResourceProxyReader(Ar, false));
            }
        }
    }
}