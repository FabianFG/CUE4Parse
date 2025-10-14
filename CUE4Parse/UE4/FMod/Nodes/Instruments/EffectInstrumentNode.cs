using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Instruments;

public class EffectInstrumentNode : BaseInstrumentNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid EffectGuid;

    public EffectInstrumentNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        EffectGuid = new FModGuid(Ar);
    }
}
