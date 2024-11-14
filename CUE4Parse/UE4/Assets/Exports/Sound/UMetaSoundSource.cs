using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public class UMetaSoundSource : SoundWaveProcedural
{
    public FStructFallback? Settings;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            Settings = new FStructFallback(Ar, "MetaSoundQualitySettings");
        }
    }
}
