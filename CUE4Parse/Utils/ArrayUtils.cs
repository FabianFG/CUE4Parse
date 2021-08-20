using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetOrFalse(this BitArray array, int index) =>
            index >= 0 && index < array.Length && array[index];

        public static void SetRangeFromRange(this BitArray array, int index, int numBitsToSet, BitArray readBits, int readOffsetBits = 0)
        {
            Trace.Assert(index >= 0 && numBitsToSet >= 0 && index + numBitsToSet <= array.Length);
            Trace.Assert(0 <= readOffsetBits && readOffsetBits + numBitsToSet <= readBits.Length);
            for (var i = 0; i < numBitsToSet; i++)
            {
                array.Set(index + i, readBits.Get(readOffsetBits + i));
            }
        }
    }
}