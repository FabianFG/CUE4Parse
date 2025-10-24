using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework;

public class FComputeKernelResourceSet
{
    public FComputeKernelResource[] KernelResources;
    
    public FComputeKernelResourceSet(FAssetArchive Ar)
    {
        KernelResources = Ar.ReadArray(() => new FComputeKernelResource(Ar));
    }
}