using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FState
{
    public string Name;
    public uint Root; // OP:ADDRESS is the actual data type, but I don't know if it is an uint or an int. This applies to UpdateCache and DynamicResources too
    public int[] RuntimeParameters;
    public uint[] UpdateCache;
    public KeyValuePair<uint, ulong>[] DynamicResources;

    public FState(FArchive Ar)
    {
        Name = Ar.ReadMutableFString();
        Root = Ar.Read<uint>();
        RuntimeParameters = Ar.ReadArray<int>();
        UpdateCache = Ar.ReadArray<uint>();
        DynamicResources = Ar.ReadArray(() => new KeyValuePair<uint, ulong>(Ar.Read<uint>(), Ar.Read<ulong>()));
    }
}
