using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine;

public class ULevelStreaming : Assets.Exports.UObject
{
    public FSoftObjectPath? WorldAsset;
    public FName? PackageName;
    public FTransform? LevelTransform { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        WorldAsset = GetOrDefault<FSoftObjectPath>(nameof(WorldAsset));
        PackageName = GetOrDefault<FName?>(nameof(PackageName));
        LevelTransform = GetOrDefault(nameof(LevelTransform), LevelTransform);
    }
}

public class ULevelStreamingDynamic : ULevelStreaming;
public class ULevelStreamingAlwaysLoaded : ULevelStreaming;
public class ULevelStreamingPersistent : ULevelStreaming;
public class ULevelStreamingLevelInstanceEditor : ULevelStreamingAlwaysLoaded;
public class ULevelStreamingLevelInstance : ULevelStreamingDynamic;
public class ULevelStreamingLevelInstanceEditorPropertyOverride : ULevelStreamingDynamic;
