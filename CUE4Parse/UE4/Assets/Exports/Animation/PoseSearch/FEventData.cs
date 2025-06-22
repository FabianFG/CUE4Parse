using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.GameplayTags;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class FEventData(FAssetArchive Ar)
{
    public Dictionary<FGameplayTag, int[]> Data = Ar.ReadMap(() => new FGameplayTag(Ar), Ar.ReadArray<int>);
}
