using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public class FOnDemandTocTagSet
{
    public string Tag;
    public FOnDemandTocTagSetPackageList[] Packages;
    
    public FOnDemandTocTagSet(FArchive Ar)
    {
        Tag = Ar.ReadFString();
        Packages = Ar.ReadArray(() => new FOnDemandTocTagSetPackageList(Ar));
    }
}