using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class StringDataNode
{
    public readonly EStringTableType Type;
    public readonly FRadixTreePacked? RadixTree;

    public StringDataNode(BinaryReader Ar)
    {
        Type = (EStringTableType) Ar.ReadUInt32();

        if (Type == EStringTableType.StringTable_RadixTree_24Bit)
        {
            RadixTree = new FRadixTreePacked(Ar, Type);
        }
    }
}
