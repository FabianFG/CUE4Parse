using CUE4Parse.UE4.Assets.Exports.Sound;
using System;
using System.Linq;

namespace CUE4Parse_Conversion.Sounds
{
    public static class Decoder
    {
        public static byte[]? Decode(this USoundWave soundWave)
        {
            if (!soundWave.bStreaming)
            {
                if (soundWave.CompressedFormatData != null) // TODO check what is the first fname
                    return soundWave.CompressedFormatData.Formats.First().Value.Data;
                else if (soundWave.RawData?.Data != null)
                    return soundWave.RawData.Data;
            }
            else if (soundWave.RunningPlatformData?.Chunks != null)
            {
                var offset = 0;
                var ret = new byte[soundWave.RunningPlatformData.Chunks.Sum(x => x.AudioDataSize)];
                for (int i = 0; i < soundWave.RunningPlatformData.NumChunks; i++)
                {
                    Buffer.BlockCopy(soundWave.RunningPlatformData.Chunks[i].BulkData.Data, 0, ret, offset, soundWave.RunningPlatformData.Chunks[i].AudioDataSize);
                    offset += soundWave.RunningPlatformData.Chunks[i].AudioDataSize;
                }
                return ret;
            }
            return null;
        }
    }
}
