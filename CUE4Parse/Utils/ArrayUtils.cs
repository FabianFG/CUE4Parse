using System;

namespace CUE4Parse.Utils
{
    public static class ArrayUtils
    {
        public static byte[] SubByteArray(this byte[] byteArray, int len)
        {
            byte[] tmp = new byte[len];
            Array.Copy(byteArray, tmp, len);

            return tmp;
        }
    }
}