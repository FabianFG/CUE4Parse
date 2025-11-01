using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Transitions;

public class TransitionRegionNode : BaseTransitionNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid DestinationGuid;
    public readonly uint Start;
    public readonly uint End;
    public readonly FLegacyParameterConditions? LegacyParameterConditions;
    public readonly List<FEvaluator> Evaluators = [];
    public readonly FQuantization Quantization;
    public readonly float TransitionChancePercent;
    public readonly uint Flags;

    public TransitionRegionNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        DestinationGuid = new FModGuid(Ar);
        Start = Ar.ReadUInt32();
        End = Ar.ReadUInt32();

        if (FModReader.Version < 0x43)
        {
            LegacyParameterConditions = new FLegacyParameterConditions(Ar);
        }
        else if (FModReader.Version >= 0x43 && FModReader.Version < 0x82)
        {
            LegacyParameterConditions = FModReader.ReadElemListImp<FLegacyParameterConditions>(Ar).FirstOrDefault();
        }
        else
        {
            Evaluators = FEvaluator.ReadEvaluatorList(Ar);
        }

        Quantization = new FQuantization(Ar);
        TransitionChancePercent = Ar.ReadSingle();

        if (FModReader.Version >= 0x7f)
        {
            Flags = Ar.ReadUInt32();
        }
        else
        {
            Flags = Ar.ReadUInt32();

            // Extra legacy flags
            if (FModReader.Version >= 0x38)
                Ar.ReadBoolean();
            if (FModReader.Version >= 0x67)
                Ar.ReadBoolean();
            if (FModReader.Version >= 0x79)
                Ar.ReadBoolean();
            if (FModReader.Version >= 0x7a)
                Ar.ReadBoolean();
            if (FModReader.Version >= 0x7b)
                Ar.ReadBoolean();
        }
    }
}
