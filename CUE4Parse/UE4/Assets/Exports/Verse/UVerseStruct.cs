using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class UVerseStruct : UScriptStruct
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        var bIsNativeCooked = Ar.Game is >= GAME_UE5_6 and < GAME_UE6_0 && Ar.ReadBoolean();
        if (!bIsNativeCooked) base.Deserialize(Ar, validPos);
    }
}
