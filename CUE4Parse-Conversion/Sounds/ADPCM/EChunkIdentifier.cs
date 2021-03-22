namespace CUE4Parse_Conversion.Sounds.ADPCM
{
    public enum EChunkIdentifier : uint
    {
        RIFF = 0x46464952,
        JUNK = 0x4B4E554A,
        WAVE = 0x45564157,
        FMT = 0x20746D66,
    }
}