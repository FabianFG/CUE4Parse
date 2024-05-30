using System.Collections.Generic;

namespace CUE4Parse.Utils;

public static class ListUtils
{
    public static T Pop<T>(this List<T> list) // dont @ me
    {
        var value = list[^1];
        list.RemoveAt(list.Count - 1);
        return value;
    }
}