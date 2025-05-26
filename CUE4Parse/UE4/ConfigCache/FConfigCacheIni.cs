using System.Collections.Generic;
using CUE4Parse.UE4.ConfigCache.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.ConfigCache;

public class FConfigCacheIni
{
    public Dictionary<string, FConfigBranch>? OtherFiles;
    public string[]? OtherFileNames;
    public FKnownConfigFiles? KnownFiles;
    public bool bAreFileOperationsDisabled;
    public bool bIsReadyForUse;
    public EConfigCacheType Type;
    public FName? PlatformName;
    public Dictionary<FName, string[]>? StagedPluginConfigCache;
    public string[]? StagedGlobalConfigCache;
    
    public FConfigCacheIni(FArchive Ar)
    {
        var num = Ar.Read<int>();
        OtherFiles = new Dictionary<string, FConfigBranch>(num);
        OtherFileNames = new string[num];
        
        for (int i = 0; i < num; i++)
        {
            var fileName = Ar.ReadFString();
            var branch = new FConfigBranch(Ar);
            
            OtherFiles[fileName] = branch;
            OtherFileNames[i] = fileName;
        }

        if (Ar.Game >= EGame.GAME_UE5_0)
            KnownFiles = new FKnownConfigFiles(Ar);
        
        bAreFileOperationsDisabled = Ar.ReadBoolean();
        bIsReadyForUse = Ar.ReadBoolean();
        Type = Ar.Read<EConfigCacheType>();

        if (Ar.Game >= EGame.GAME_UE5_5)
        {
            PlatformName = Ar.ReadFName();
        }

        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            StagedPluginConfigCache = Ar.ReadMap(Ar.ReadFName, () => Ar.ReadArray(Ar.ReadFString));

            var bHasGlobalCache = Ar.ReadBoolean();
            if (bHasGlobalCache)
            {
                StagedGlobalConfigCache = Ar.ReadArray(Ar.ReadFString);
            }
        }
    }
}

public enum EConfigCacheType : byte
{
    // this type of config cache will write its files to disk during Flush (i.e. GConfig)
    DiskBacked,
    // this type of config cache is temporary and will never write to disk (only load from disk)
    Temporary,
}