using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class PropertyNode
{
    public readonly uint Index;
    public readonly ushort Method;
    public readonly ushort Type;
    public readonly FModGuid MappingGuid;
    public readonly FModGuid[] Controllers;
    public readonly FModGuid[] Modulators;

    public PropertyNode(BinaryReader Ar)
    {
        Index = Ar.ReadUInt32();
        Method = Ar.ReadUInt16();
        Type = Ar.ReadUInt16();
        MappingGuid = new FModGuid(Ar);
        Controllers = FModReader.ReadElemListImp<FModGuid>(Ar);
        Modulators = FModReader.ReadElemListImp<FModGuid>(Ar);
    }
}
