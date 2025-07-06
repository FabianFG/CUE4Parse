using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.BinaryConfig.Objects;

public class FConfigFileHierarchy
{
    public Dictionary<int, string> ConfigFileHierarchyMap;
    public int KeyGen;

    public FConfigFileHierarchy(FArchive Ar)
    {
        ConfigFileHierarchyMap = Ar.ReadMap(Ar.Read<int>, Ar.ReadFString);
        KeyGen = Ar.Read<int>();
    }
}
