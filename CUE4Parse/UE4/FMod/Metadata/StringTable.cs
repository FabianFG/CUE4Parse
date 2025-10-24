using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Metadata;

// String table is used to convert event FModGuids to human readable names
public class StringTable
{
    public readonly EStringTableType Type;
    public readonly FRadixTreePacked? RadixTree;

    public StringTable(BinaryReader Ar)
    {
        Type = (EStringTableType)Ar.ReadUInt32();

        if (Type == EStringTableType.StringTable_RadixTree_24Bit)
        {
            RadixTree = new FRadixTreePacked(Ar, Type);
        }
    }
}
