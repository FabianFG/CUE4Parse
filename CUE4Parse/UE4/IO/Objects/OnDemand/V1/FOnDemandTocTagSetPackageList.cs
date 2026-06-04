using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FOnDemandTocTagSetPackageList
{
    public uint ContainerIndex;
    public uint[] PackageIndices;

    public FOnDemandTocTagSetPackageList(FArchive Ar)
    {
        ContainerIndex = Ar.Read<uint>();
        PackageIndices = Ar.ReadArray<uint>();
    }
}