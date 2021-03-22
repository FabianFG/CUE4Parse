namespace CUE4Parse_Conversion.Sounds.ADPCM
{
    public enum EAudioFormat : ushort
    {
        WAVE_FORMAT_UNKNOWN = 0x0000,
        WAVE_FORMAT_PCM = 0x0001,
        WAVE_FORMAT_ADPCM = 0x0002,
        WAVE_FORMAT_IEEE_FLOAT = 0x0003,
        WAVE_FORMAT_VSELP = 0x0004,
        WAVE_FORMAT_IBM_CVSD = 0x0005,
        WAVE_FORMAT_ALAW = 0x0006,
        WAVE_FORMAT_MULAW = 0x0007,
        WAVE_FORMAT_OKI_ADPCM = 0x0010,
        WAVE_FORMAT_DVI_ADPCM = 0x0011,
        WAVE_FORMAT_MEDIASPACE_ADPCM = 0x0012,
        WAVE_FORMAT_SIERRA_ADPCM = 0x0013,
        WAVE_FORMAT_G723_ADPCM = 0x0014,
        
        WAVE_FORMAT_EXTENSIBLE = 0xFFFE // Determined by SubFormat
    }
}