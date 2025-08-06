using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine;

public class ULevelStreaming : Assets.Exports.UObject
{
    public FSoftObjectPath? WorldAsset;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        WorldAsset = GetOrDefault<FSoftObjectPath>(nameof(WorldAsset));
    }
}

public class ULevelStreamingDynamic : ULevelStreaming;