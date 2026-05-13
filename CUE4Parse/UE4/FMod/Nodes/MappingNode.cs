using CUE4Parse.UE4.FMod.Objects;
using Newtonsoft.Json;
using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes;

public class MappingNode
{
    [JsonIgnore] public readonly FModGuid BaseGuid;
    public readonly FMappingPoint[] MappingPoints;

    public MappingNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        MappingPoints = FModReader.ReadElemListImp<FMappingPoint>(Ar);
    }
}
