using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine.InstanceData;

public struct FPrimitiveInstanceId
{
    public int Id = -1;

    public FPrimitiveInstanceId(FArchive Ar)
    {
        Id = Ar.Read<int>();
    }
}
