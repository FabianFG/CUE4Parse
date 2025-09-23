using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ComputerFramework;

public class UComputeGraph : UObject
{
    public FComputeKernelResourceSet[] KernelResources;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        KernelResources = Ar.ReadArray(() => new FComputeKernelResourceSet(Ar));
    }
}