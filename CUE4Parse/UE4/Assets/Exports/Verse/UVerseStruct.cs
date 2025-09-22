using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class UVerseStruct : UScriptStruct
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        var bIsNativeCooked = Ar.Game >= EGame.GAME_UE5_6 && Ar.ReadBoolean();
        if (!bIsNativeCooked) base.Deserialize(Ar, validPos);
    }
}