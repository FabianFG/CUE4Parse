using System;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.PCG;

public class FPCGPointArrayProperty<T> where T : struct
{
    // Array containing values if allocated
    public T[] Values;
    // Value representing all values if array is unallocated
    public T Value;
    // Number of values represented by this FPCGPointArrayProperty
    public int NumValues;

    public FPCGPointArrayProperty(FAssetArchive Ar)
    {
        NumValues = Ar.Read<int>();
        Value = Ar.Read<T>();
        Values = Ar.ReadArray<T>();
    }

    public FPCGPointArrayProperty(FAssetArchive Ar, Func<T> getter)
    {
        NumValues = Ar.Read<int>();
        Value = getter();
        Values = Ar.ReadArray(getter);
    }
}
