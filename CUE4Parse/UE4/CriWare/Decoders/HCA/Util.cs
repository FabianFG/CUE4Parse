using System;

namespace CUE4Parse.UE4.CriWare.Decoders.HCA;

internal static class Util
{
    public static float UInt32ToSingle(uint value) => BitConverter.ToSingle(BitConverter.GetBytes(value), 0);

    public static void Fill<T>(Array array, T value, int startIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            array.SetValue(value, i + startIndex);
        }
    }
}
