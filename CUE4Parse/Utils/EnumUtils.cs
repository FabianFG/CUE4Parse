using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CUE4Parse.Utils;

public static class EnumUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStringBitfield<T>(this T inEnum) where T : Enum
    {
        var outValues = ((T[]) Enum.GetValues(typeof(T))).Where(enumValue => Convert.ToUInt64(enumValue) == 0 ? Convert.ToUInt64(inEnum) == 0 : inEnum.HasFlag(enumValue)).ToList();
        return outValues.Count > 0 ? string.Join(" | ", outValues) : "0";
    }
}
