using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node
{
    public class USoundNodeWavePlayer : USoundNode
    {
        public FPackageIndex? SoundWave;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.HardSoundReferences)
            {
                SoundWave = new FPackageIndex(Ar);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (SoundWave == null) return;
            writer.WritePropertyName("SoundWave");
            serializer.Serialize(writer, SoundWave);
        }
    }
}
