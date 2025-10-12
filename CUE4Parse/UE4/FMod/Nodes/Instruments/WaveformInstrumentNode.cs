using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class WaveformInstrumentNode : BaseInstrumentNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid WaveformResourceGuid;

    public WaveformInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        WaveformResourceGuid = new FModGuid(Ar);
    }
}
