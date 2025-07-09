using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.BinaryConfig.Objects;

public struct FKnownConfigFiles
{
    public FConfigBranch[] Branches;

    public FKnownConfigFiles(FArchive Ar)
    {
        Branches = Ar.ReadArray((int)EKnownIniFile.NumKnownFiles, () => new FConfigBranch(Ar));
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

    // convenient counter for the above list
    NumKnownFiles,
}
