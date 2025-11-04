using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.BinaryConfig.Objects;

public class FConfigFileHierarchy
{
    public Dictionary<int, string> ConfigFileHierarchyMap;
    public int KeyGen;

    public FConfigFileHierarchy(FArchive Ar)
    {
        ConfigFileHierarchyMap = Ar.ReadMap(Ar.Read<int>, () => Ar.Game >= EGame.GAME_UE5_7 ? Ar.ReadFUtf8String() : Ar.ReadFString());
        if (Ar.Game < EGame.GAME_UE5_8) KeyGen = Ar.Read<int>();
    }
}
