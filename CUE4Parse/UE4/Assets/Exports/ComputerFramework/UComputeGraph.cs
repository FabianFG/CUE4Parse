using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework;

public class UComputeGraph : UObject
{
    public FComputeKernelResourceSet[] KernelResources;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var numKernels = Ar.Read<int>();
        KernelResources = new FComputeKernelResourceSet[numKernels];

        for (int i = 0; i < numKernels; i++)
        {
            KernelResources[i] = new FComputeKernelResourceSet(Ar);
        }
    }
}