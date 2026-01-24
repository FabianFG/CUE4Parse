using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

public class USoundWaveProcedural : USoundWave
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        SoundBaseDeserialize(Ar, validPos);
    }

    protected override void SerializeCuePoints(FAssetArchive Ar) { }
}
