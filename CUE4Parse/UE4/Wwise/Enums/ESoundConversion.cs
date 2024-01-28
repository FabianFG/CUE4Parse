using System;

namespace CUE4Parse.UE4.Wwise.Enums
{
    [Flags]
    public enum ESoundConversion : byte
    {
        PCM    = 1 << 0,   // 1
        ADPCM  = 1 << 1,   // 2
        Vorbis = 1 << 2    // 4
    }
}
