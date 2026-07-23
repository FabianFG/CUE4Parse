using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NetEase.MAR.Assets.Exports.Animation;

public class UAnimNotifyState_TimedHideMaterial : UAnimNotifyState
{
    public bool bHideAllSections;
    public int[]? HideMaterialIDArray;
    public FName?[]? HideMaterialSlotSuffixes;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bHideAllSections = GetOrDefault<bool>(nameof(bHideAllSections));
        HideMaterialIDArray = GetOrDefault<int[]?>(nameof(HideMaterialIDArray));
        HideMaterialSlotSuffixes = GetOrDefault<FName?[]?>(nameof(HideMaterialSlotSuffixes));
    }
}
