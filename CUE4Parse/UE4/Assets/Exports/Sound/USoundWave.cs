using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public class USoundWave : USoundBase
    {
        public bool bStreaming { get; private set; } = true;
        public FFormatContainer? CompressedFormatData { get; private set; }
        public FByteBulkData? RawData { get; private set; }
        public FGuid CompressedDataGuid { get; private set; }
        public FStreamedAudioPlatformData? RunningPlatformData { get; private set; }

        public USoundWave() { }
        public USoundWave(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            // UObject Properties
            if (GetOrDefault<bool>(nameof(bStreaming))) // will return false if not found
                bStreaming = true;
            else
                bStreaming = Ar.Game >= EGame.GAME_UE4_25; // recheck if false

            bool bCooked = Ar.ReadBoolean();
            if (!bStreaming)
            {
                if (bCooked)
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
    }
}
