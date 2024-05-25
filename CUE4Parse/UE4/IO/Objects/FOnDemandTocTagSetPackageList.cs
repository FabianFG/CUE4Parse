using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public class FOnDemandTocTagSetPackageList
{
    public uint ContainerIndex;
    public uint[] PackageIndicies;
    
    public FOnDemandTocTagSetPackageList(FArchive Ar)
    {
        ContainerIndex = Ar.Read<uint>();
        PackageIndicies = Ar.ReadArray<uint>();
    }
}