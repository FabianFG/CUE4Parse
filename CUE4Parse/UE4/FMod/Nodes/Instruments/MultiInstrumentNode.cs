using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class MultiInstrumentNode : BaseInstrumentNode
{
    public readonly FModGuid BaseGuid;
    public PlaylistNode? PlaylistBody;

    public MultiInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
    }
}
