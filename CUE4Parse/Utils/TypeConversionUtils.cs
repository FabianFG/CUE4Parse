using System;

namespace CUE4Parse.Utils
{
    public static class TypeConversionUtils
    {
        public static float HalfToFloat(ushort h)
        {
            var sign = (h >> 15) & 0x00000001;
            var exp  = (h >> 10) & 0x0000001F;
            var mant =  h        & 0x000003FF;

            exp += 127 - 15;
            return BitConverter.ToSingle(BitConverter.GetBytes((sign << 31) | (exp << 23) | (mant << 13)));
        }
    }
}