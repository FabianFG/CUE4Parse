using CUE4Parse.UE4.Assets.Exports.Sound;
using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Wwise;

namespace CUE4Parse_Conversion.Sounds
{
    public static class Decoder
    {
        public static byte[]? Decode(this USoundWave soundWave)
        {
            if (!soundWave.bStreaming)
            {
                if (soundWave.CompressedFormatData != null)
                    return soundWave.CompressedFormatData.Formats.First().Value.Data;
                if (soundWave.RawData?.Data != null)
                    return soundWave.RawData.Data;
            }
            else if (soundWave.RunningPlatformData?.Chunks != null)
            {
                var offset = 0;
                var ret = new byte[soundWave.RunningPlatformData.Chunks.Sum(x => x.AudioDataSize)];
                for (var i = 0; i < soundWave.RunningPlatformData.NumChunks; i++)
                {
                    Buffer.BlockCopy(soundWave.RunningPlatformData.Chunks[i].BulkData.Data, 0, ret, offset, soundWave.RunningPlatformData.Chunks[i].AudioDataSize);
                    offset += soundWave.RunningPlatformData.Chunks[i].AudioDataSize;
                }
                return ret;
            }
            return null;
        }
        
        public static byte[] Decode(this UAkMediaAssetData mediaData)
        {
            var offset = 0;
            var ret = new byte[mediaData.DataChunks.Sum(x => x.Data.Data.Length)];
            for (var i = 0; i < mediaData.DataChunks.Length; i++)
            {
                Buffer.BlockCopy(mediaData.DataChunks[i].Data.Data, 0, ret, offset, mediaData.DataChunks[i].Data.Data.Length);
                offset += mediaData.DataChunks[i].Data.Data.Length;
            }
            return ret;
        }
    }
}
