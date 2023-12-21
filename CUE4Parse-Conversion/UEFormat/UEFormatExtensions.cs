using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.UEFormat.Structs;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.UEFormat;

public static class UEFormatExtensions
{
    public static void WriteFString(this FArchiveWriter Ar, string str)
    {
        new FString(str).Serialize(Ar);
    }

    public static void WriteArray<T>(this FArchiveWriter Ar, IEnumerable<T> enumerable) where T : ISerializable
    {
        var array = enumerable.ToArray();
        Ar.WriteArray(array, it => it.Serialize(Ar));
    }
    
    public static void WriteArray<T>(this FArchiveWriter Ar, IEnumerable<T> enumerable, Action<T> action)
    {
        var items = enumerable.ToArray();
        
        Ar.Write(items.Length);
        foreach (var item in items)
        {
            action(item);
        }
    }
    
    public static void WriteArray<T>(this FArchiveWriter Ar, IEnumerable<T> enumerable, Action<FArchiveWriter, T> action)
    {
        var items = enumerable.ToArray();
        
        Ar.Write(items.Length);
        foreach (var item in items)
        {
            action(Ar, item);
        }
    }
}