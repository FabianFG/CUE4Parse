using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FState
{
    public string Name;
    public uint Root;
    public int[] RuntimeParameters;
    public uint[] UpdateCache;
    public KeyValuePair<uint, ulong>[] DynamicResources;

    public FState(FMutableArchive Ar)
    {
        Name = Ar.ReadFString();
        Root = Ar.Read<uint>();
        RuntimeParameters = Ar.ReadArray<int>();
        UpdateCache = Ar.ReadArray<uint>();
        DynamicResources = Ar.ReadArray(() => new KeyValuePair<uint, ulong>(Ar.Read<uint>(), Ar.Read<ulong>()));
    }
}