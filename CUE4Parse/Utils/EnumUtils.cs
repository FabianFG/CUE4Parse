using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CUE4Parse.Utils;

public static class EnumUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStringBitfield<T>(this T inEnum) where T : Enum
    {
        var outValues = new List<T>();
        foreach (var enumValue in (T[]) Enum.GetValues(typeof(T)))
        {
            if (Convert.ToUInt64(enumValue) == 0 ? Convert.ToUInt64(inEnum) == 0 : inEnum.HasFlag(enumValue))
            {
                outValues.Add(enumValue);
            }
        }
        return outValues.Count > 0 ? string.Join(" | ", outValues) : "0";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetValueByName<T>(string name) where T : Enum
    {
        var start = name.IndexOf("::", StringComparison.Ordinal);
        start = start == -1 ? 0 : start + 2;
        return (T) Enum.Parse(typeof(T), name.AsSpan(start));
    }
}
