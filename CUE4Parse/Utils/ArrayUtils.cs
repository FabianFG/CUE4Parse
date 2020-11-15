using System;
using System.Collections;

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
        
        public static bool Contains(this BitArray array, bool search)
        {
            for (var i = 0; i < array.Count; i++)
            {
                if (array[i])
                    return true;
            }

            return false;
        }
    }
}