using System.Collections.Generic;
using CUE4Parse.UE4.BinaryConfig.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.BinaryConfig;

public class FConfigCacheIni
{
    public Dictionary<string, FConfigBranch> OtherFiles;
    public string[] OtherFileNames;
    public FKnownConfigFiles KnownFiles;
    public bool bAreFileOperationsDisabled;
    public bool bIsReadyForUse;
    public EConfigCacheType Type;
    public FName PlatformName;
    public Dictionary<FName, string[]> StagedPluginConfigCache;
    public string[]? StagedGlobalConfigCache;

    public FConfigCacheIni(FArchive Ar)
    {
        var num = Ar.Read<int>();
        OtherFiles = new Dictionary<string, FConfigBranch>(num);
        OtherFileNames = new string[num];
        for (var i = 0; i < num; i++)
        {
            var fileName = Ar.ReadFString();
            var branch = new FConfigBranch(Ar);

            OtherFiles[fileName] = branch;
            OtherFileNames[i] = fileName;
        }

        KnownFiles = new FKnownConfigFiles(Ar);
        bAreFileOperationsDisabled = Ar.ReadBoolean();
        bIsReadyForUse = Ar.ReadBoolean();
        Type = Ar.Read<EConfigCacheType>();
        PlatformName = Ar.ReadFName();
        StagedPluginConfigCache = Ar.ReadMap(Ar.ReadFName, () => Ar.ReadArray(Ar.ReadFString));

        var bHasGlobalCache = Ar.ReadBoolean();
        if (bHasGlobalCache) StagedGlobalConfigCache = Ar.ReadArray(Ar.ReadFString);

        // there are 4 bytes at the end and I don't know what it is
    }
}

[JsonConverter(typeof(EnumConverter<EConfigCacheType>))]
public enum EConfigCacheType : byte
{
    // this type of config cache will write its files to disk during Flush (i.e. GConfig)
    DiskBacked,
    // this type of config cache is temporary and will never write to disk (only load from disk)
    Temporary,
}
