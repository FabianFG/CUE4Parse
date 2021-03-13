using System;

namespace CUE4Parse.UE4.Wwise.Enums
{
    [Flags]
    public enum ESoundConversion : byte
    {
        PCM    = 0b_0001,   // 1
        ADPCM  = 0b_0010,   // 2
        Vorbis = 0b_0100    // 4
    }
}