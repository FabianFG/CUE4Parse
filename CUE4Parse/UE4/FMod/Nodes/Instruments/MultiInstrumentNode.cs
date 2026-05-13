using System.IO;
using CUE4Parse.UE4.FMod.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class MultiInstrumentNode : BaseInstrumentNode
{
    [JsonIgnore] public readonly FModGuid BaseGuid;
    public PlaylistNode? PlaylistBody;

    public MultiInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
    }
}
