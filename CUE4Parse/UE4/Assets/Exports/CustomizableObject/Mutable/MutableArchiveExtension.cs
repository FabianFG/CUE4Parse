using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public static class MutableArchiveExtension
{
    public static string ReadMutableFString(this FArchive Ar)
    {
        var length = Ar.Read<int>() * 2;
        return Ar.ReadStringUnsafe(length).Replace("\0", string.Empty);
    }

    public static T[] ReadMutableArray<T>(this FArchive Ar, Func<T> getter) where T : IMutablePtr
    {
        var length = Ar.Read<int>();
        var result = new List<T>();

        for (var i = 0; i < length; i++)
        {
            var index = Ar.Read<int>();
            if (index == -1)
            {
                i--;
                continue;
            }

            var item = getter();
            if (item.IsBroken)
                continue;
            
            result.Add(item);
        }

        return result.ToArray();
    }
}