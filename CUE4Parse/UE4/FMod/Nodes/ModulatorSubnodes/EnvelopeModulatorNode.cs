using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

public class EnvelopeModulatorNode
{
    public readonly float? Amount;
    public readonly float ThresholdMinimum;
    public readonly float ThresholdMaximum;
    public readonly float? AttackTime;
    public readonly float? ReleaseTime;
    public readonly bool? UseRMS;
    public readonly float? Minimum;
    public readonly float? Maximum;
    public readonly FModGuid? EffectId;

    public EnvelopeModulatorNode(BinaryReader Ar)
    {
        if (FModReader.Version >= 0x55)
        {
            Amount = Ar.ReadSingle();
        }
        else
        {
            Minimum = Ar.ReadSingle();
            Maximum = Ar.ReadSingle();
        }

        ThresholdMinimum = Ar.ReadSingle();
        ThresholdMaximum = Ar.ReadSingle();

        if (FModReader.Version >= 0x53)
        {
            AttackTime = Ar.ReadSingle();
            ReleaseTime = Ar.ReadSingle();

            if (FModReader.Version >= 0x7d)
            {
                UseRMS = Ar.ReadBoolean();
            }
        }
        else
        {
            EffectId = new FModGuid(Ar);
        }
    }
}
