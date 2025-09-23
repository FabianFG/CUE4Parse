using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework;

public class FComputeKernelResourceSet
{
    public FComputeKernelResource[] KernelResources;
    
    public FComputeKernelResourceSet(FAssetArchive Ar)
    {
        var numResources = Ar.Read<int>();
        
        KernelResources = new FComputeKernelResource[numResources];
        for (int i = 0; i < KernelResources.Length; i++)
        {
            KernelResources[i] = new FComputeKernelResource(Ar);
        }
    }
}