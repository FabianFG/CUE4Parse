using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.ConfigCache.Objects;

public class FConfigBranch
{
    public bool bIsHierarchical;
    public FConfigFile InMemoryFile;
    public FConfigFileHierarchy Hierarchy;
    public FConfigFile CombinedStaticLayers;
    public FConfigFile FinalCombinedLayers;
    public FName IniName;
    public string IniPath;
    
    public FConfigBranch(FArchive Ar)
    {
        bIsHierarchical = Ar.ReadBoolean();
        InMemoryFile = new FConfigFile(Ar);
        Hierarchy = new FConfigFileHierarchy(Ar);
        CombinedStaticLayers = new FConfigFile(Ar);
        FinalCombinedLayers = new FConfigFile(Ar);
        IniName = Ar.ReadFName();
        IniPath = Ar.ReadFString();
    }
}