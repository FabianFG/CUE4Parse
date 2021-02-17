using System.Runtime.CompilerServices;

namespace CUE4Parse.Utils
{
    public static class UnsafePrint
    {
        public static unsafe string BytesToHex(byte* bytes, uint length)
        {
            char[] c = new char[length * 2];

            byte b;

            for(int bx = 0, cx = 0; bx < length; ++bx, ++cx)
            {
                b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b - 10 + 'A' : b + '0');

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b - 10 + 'A' : b + '0');
            }

            return new string(c);
        }
        
        public static unsafe string BytesToHex(params byte[] bytes)
        {
            var result = string.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                result += BytesToHex((byte*) Unsafe.AsPointer(ref bytes[i]), 1);
            }
            return result;
        }
    }
}