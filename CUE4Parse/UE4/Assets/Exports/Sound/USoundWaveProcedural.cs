using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public class SoundWaveProcedural : USoundWave
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.SoundBaseDeserialize(Ar, validPos);
    }

    public override void SerializeCuePoints(FAssetArchive Ar) { }
}
