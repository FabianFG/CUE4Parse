using System.Collections.Generic;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using FConfigFileMap = System.Collections.Generic.Dictionary<string, CUE4Parse.UE4.BinaryConfig.Objects.FConfigSection>;

namespace CUE4Parse.UE4.BinaryConfig.Objects;

public class FConfigFile
{
    public FConfigFileMap ConfigFileMap;
    public bool Dirty;
    public bool NoSave;
    public bool bHasPlatformName;
    public FName Name;
    public string PlatformName;
    public Dictionary<string, Dictionary<FName, string>> PerObjectConfigArrayOfStructKeys;

    public FConfigFile(FArchive Ar)
    {
        ConfigFileMap = Ar.ReadMap(Ar.ReadFString, () => new FConfigSection(Ar));
        Dirty = Ar.ReadBoolean();
        NoSave = Ar.ReadBoolean();
        bHasPlatformName = Ar.ReadBoolean();
        Name = Ar.ReadFName();
        PlatformName = Ar.ReadFString();
        PerObjectConfigArrayOfStructKeys = Ar.ReadMap(Ar.ReadFString, () => Ar.ReadMap(Ar.ReadFName, Ar.ReadFString));
    }
}
