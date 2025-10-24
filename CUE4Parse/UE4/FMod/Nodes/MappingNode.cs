using CUE4Parse.UE4.FMod.Objects;
using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes;

public class MappingNode
{
    public readonly FModGuid BaseGuid;
    public readonly FMappingPoint[] MappingPoints;

    public MappingNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        MappingPoints = FModReader.ReadElemListImp<FMappingPoint>(Ar);
    }
}
