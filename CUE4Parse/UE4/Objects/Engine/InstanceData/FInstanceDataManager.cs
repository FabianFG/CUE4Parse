using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine.InstanceData;

public class FInstanceDataManager(FArchive Ar) : FInstanceIdIndexMap(Ar)
{
    public FPrecomputedInstanceSpatialHashData? PrecomputedOptimizationData;

    public void ReadCookedRenderData(FArchive Ar)
    {
        var bHasCookedData = Ar.ReadBoolean();
        if (bHasCookedData)
        {
            PrecomputedOptimizationData = new FPrecomputedInstanceSpatialHashData(Ar);
        }
    }
}
