using CUE4Parse.UE4.FMod.Objects;
using System.Collections.Generic;
using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.Transitions;

public class TransitionRegionNode : BaseTransitionNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid DestinationGuid;
    public readonly uint Start;
    public readonly uint End;
    public readonly List<FEvaluator> Evaluators;
    public readonly FQuantization Quantization;
    public readonly float TransitionChancePercent;
    public readonly uint Flags;

    public TransitionRegionNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        DestinationGuid = new FModGuid(Ar);
        Start = Ar.ReadUInt32();
        End = Ar.ReadUInt32();
        Evaluators = FEvaluator.ReadEvaluatorList(Ar);
        Quantization = new FQuantization(Ar);
        TransitionChancePercent = Ar.ReadSingle();
        Flags = Ar.ReadUInt32();
    }
}
