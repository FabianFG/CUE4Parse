using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Buses;

public class BaseBusNode
{
    public readonly FModGuid BaseGuid;
    public readonly FRoutable Routable;
    public BusNode? BusBody;

    public BaseBusNode(BinaryReader Ar, bool includeRoutable)
    {
        BaseGuid = new FModGuid(Ar);
        if (includeRoutable) Routable = new FRoutable(Ar);
    }
}
