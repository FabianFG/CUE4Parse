using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse_Conversion.Sounds.ADPCM
{
    /// <summary>
    /// http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html
    /// </summary>
    public static class ADPCMDecoder
    {
        public static EAudioFormat GetAudioFormat(FArchive Ar)
        {
            var rfId = Ar.Read<EChunkIdentifier>();
            if (rfId != EChunkIdentifier.RIFF)
                throw new Exception($"Invalid RIFF identifier (should be {EChunkIdentifier.RIFF} but actually is {rfId})");

            var fileSize = Ar.Read<uint>();
            
            var wvId = Ar.Read<EChunkIdentifier>();
            if (wvId != EChunkIdentifier.WAVE)
                throw new Exception($"Invalid WAVE identifier (should be {EChunkIdentifier.WAVE} but actually is {wvId})");
            
            var ftId = Ar.Read<EChunkIdentifier>();
            if (ftId != EChunkIdentifier.FMT)
                throw new Exception($"Invalid FMT identifier (should be {EChunkIdentifier.FMT} but actually is {ftId})");

            var ftSize = Ar.Read<uint>();
            var savePos = Ar.Position;
            var wFormatTag = Ar.Read<EAudioFormat>();
            var nChannels = Ar.Read<ushort>();
            var nSamplesPerSec = Ar.Read<uint>();
            var nAvgBytesPerSec = Ar.Read<uint>();
            var nBlockAlign = Ar.Read<ushort>();
            var wBitsPerSample = Ar.Read<ushort>();
            if (wFormatTag <= EAudioFormat.WAVE_FORMAT_PCM)
            {
                Ar.Position = savePos + ftSize;
                return wFormatTag;
            }
            
            var cbSize = Ar.Read<ushort>();
            if (wFormatTag < EAudioFormat.WAVE_FORMAT_EXTENSIBLE)
            {
                Ar.Position = savePos + ftSize;
                return wFormatTag;
            }

            if (wBitsPerSample != (8 * nBlockAlign / nChannels))
                throw new Exception("The original bits/sample field does not match the container size");
            
            var wValidBitsPerSample = Ar.Read<ushort>();
            var dwChannelMask = Ar.Read<uint>();
            wFormatTag = Ar.Read<EAudioFormat>();
            Ar.Position -= sizeof(EAudioFormat);
            var subFormat = Ar.Read<FGuid>();

            Ar.Position = savePos + ftSize;
            return wFormatTag;
        }
    }
}