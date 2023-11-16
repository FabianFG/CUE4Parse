using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public enum ESoundWaveFlag : uint
    {
        CookedFlag					= 1 << 0,
        HasOwnerLoadingBehaviorFlag	= 1 << 1,
        LoadingBehaviorShift		= 2,
        LoadingBehaviorMask			= 0b00000111,
    }

    public class USoundWave : USoundBase
    {
        public bool bStreaming { get; private set; } = true;
        public FFormatContainer? CompressedFormatData { get; private set; }
        public FByteBulkData? RawData { get; private set; }
        public FGuid CompressedDataGuid { get; private set; }
        public FStreamedAudioPlatformData? RunningPlatformData { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            bStreaming = Ar.Versions["SoundWave.UseAudioStreaming"];
            if (TryGetValue(out bool s, nameof(bStreaming))) // will return false if not found
                bStreaming = s;
            else if (TryGetValue(out FName loadingBehavior, "LoadingBehavior"))
                bStreaming = !loadingBehavior.IsNone && loadingBehavior.Text != "ESoundWaveLoadingBehavior::ForceInline";

            var flags = Ar.Read<ESoundWaveFlag>();
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.SOUND_COMPRESSION_TYPE_ADDED && FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.RemoveSoundWaveCompressionName)
            {
                Ar.ReadFName(); // DummyCompressionName
            }

            if (Ar.Game >= EGame.GAME_UE5_4 && flags.HasFlag(ESoundWaveFlag.CookedFlag))
            {
                Ar.ReadArray(Ar.ReadFString); // PlatformCuePoints
            }

            if (!bStreaming)
            {
                if (flags.HasFlag(ESoundWaveFlag.CookedFlag))
                {
                    CompressedFormatData = new FFormatContainer(Ar);
                }
                else
                {
                    RawData = new FByteBulkData(Ar);
                }

                CompressedDataGuid = Ar.Read<FGuid>();
            }
            else
            {
                CompressedDataGuid = Ar.Read<FGuid>();
                RunningPlatformData = new FStreamedAudioPlatformData(Ar);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("CompressedFormatData");
            serializer.Serialize(writer, CompressedFormatData);

            writer.WritePropertyName("RawData");
            serializer.Serialize(writer, RawData);

            writer.WritePropertyName("CompressedDataGuid");
            serializer.Serialize(writer, CompressedDataGuid);

            writer.WritePropertyName("RunningPlatformData");
            serializer.Serialize(writer, RunningPlatformData);
        }
    }
}
