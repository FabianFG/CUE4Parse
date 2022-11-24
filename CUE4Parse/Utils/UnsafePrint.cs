using System;

namespace CUE4Parse.Utils
{
    public static class UnsafePrint
    {
        public static unsafe string BytesToHex(byte* bytes, uint length)
        {
            var c = new char[length * 2];

            for (int bx = 0, cx = 0; bx < length; ++bx, ++cx)
            {
                var b = (byte) (bytes[bx] >> 4);
                c[cx] = (char) (b > 9 ? b - 10 + 'A' : b + '0');

                b = (byte) (bytes[bx] & 0x0F);
                c[++cx] = (char) (b > 9 ? b - 10 + 'A' : b + '0');
            }

            return new string(c);
        }

        public static string BytesToHex(params byte[] bytes)
        {
            var hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
        }
    }
}
