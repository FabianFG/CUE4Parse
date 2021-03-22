using CUE4Parse.UE4.Assets.Exports.Sound;
using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse_Conversion.Sounds.ADPCM;

namespace CUE4Parse_Conversion.Sounds
{
    public static class Decoder
    {
        public static void Decode(this USoundWave soundWave, out string audioFormat, out byte[]? data)
        {
            audioFormat = string.Empty;
            byte[]? input = null;
            
            if (!soundWave.bStreaming)
            {
                if (soundWave.CompressedFormatData != null)
                {
                    var compressedData = soundWave.CompressedFormatData.Formats.First();
                    audioFormat = compressedData.Key.Text;
                    input = compressedData.Value.Data;
                }

                if (soundWave.RawData?.Data != null) // is this even a thing?
                {
                    audioFormat = string.Empty;
                    input = soundWave.RawData.Data;
                }
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
                
                audioFormat = soundWave.RunningPlatformData.AudioFormat.Text;
                input = ret;
            }

            data = Parse(ref audioFormat, input);
        }
        
        public static byte[] Decode(this UAkMediaAssetData mediaData)
        {
            var offset = 0;
            var ret = new byte[mediaData.DataChunks.Sum(x => x.Data.Data.Length)];
            foreach (var dataChunk in mediaData.DataChunks)
            {
                Buffer.BlockCopy(dataChunk.Data.Data, 0, ret, offset, dataChunk.Data.Data.Length);
                offset += dataChunk.Data.Data.Length;
            }
            return ret;
        }

        private static byte[]? Parse(ref string audioFormat, byte[]? input)
        {
            if (input == null) return null;
            if (audioFormat.Equals("ADPCM", StringComparison.OrdinalIgnoreCase))
            {
                audioFormat = "WAV";
                switch (ADPCMDecoder.GetAudioFormat(input))
                {
                    case EAudioFormat.WAVE_FORMAT_PCM:
                        return input;
                    case EAudioFormat.WAVE_FORMAT_ADPCM:
                        return null;
                }
            }
            else if (audioFormat.Equals("OPUS", StringComparison.OrdinalIgnoreCase))
                return null;
            else if (audioFormat.IndexOf("OGG", StringComparison.OrdinalIgnoreCase) > -1)
            {
                audioFormat = "OGG";
                return input;
            }
            
            return null;
        }
    }
}
