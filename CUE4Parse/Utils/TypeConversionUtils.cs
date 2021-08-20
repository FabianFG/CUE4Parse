using static System.BitConverter;

namespace CUE4Parse.Utils
{
    public static class TypeConversionUtils
    {
        public static float HalfToFloat(ushort fp16)
        {
            const uint shiftedExp = 0x7c00 << 13;		// exponent mask after shift
            var magic = Int32BitsToSingle(113 << 23);

            var fp32 = (fp16 & 0x7fff) << 13;			// exponent/mantissa bits
            var exp = shiftedExp & fp32;				// just the exponent
            fp32 += (127 - 15) << 23;					// exponent adjust

            // handle exponent special cases
            if (exp == shiftedExp)						// Inf/NaN?
            {
                fp32 += (128 - 16) << 23;				// extra exp adjust
            }
            else if (exp == 0)							// Zero/Denormal?
            {
                fp32 += 1 << 23;						// extra exp adjust
                fp32 = SingleToInt32Bits(Int32BitsToSingle(fp32) - magic); // renormalize
            }

            fp32 |= (fp16 & 0x8000) << 16;				// sign bit
            var halfToFloat = Int32BitsToSingle(fp32);
            return halfToFloat;
        }
    }
}