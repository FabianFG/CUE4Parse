using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.ConfigCache.Objects;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FKnownConfigFiles
{
    public FConfigBranch[] Branches = new FConfigBranch[(int) EKnownIniFile.NumKnownFiles];
    
    public FKnownConfigFiles(FArchive Ar)
    {
        for (int i = 0; i < Branches.Length; i++)
        {
            Branches[i] = new FConfigBranch(Ar);
        }
    }
}

public enum EKnownIniFile : byte
{
    Engine,
    Game,
    Input,
    DeviceProfiles,
    GameUserSettings,
    Scalability,
    RuntimeOptions,
    InstallBundle,
    Hardware,
    GameplayTags,
    NumKnownFiles,
}