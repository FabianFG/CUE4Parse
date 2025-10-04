using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes.ModulatorSubnodes;

/// ADSR (Attack, Decay, Sustain, Release) modulator
public class ADSRModulatorNode
{
    public readonly float InitialValue;
    public readonly float PeakValue;
    public readonly float SustainValue;
    public readonly float AttackTime;
    public readonly float HoldTime;
    public readonly float DecayTime;
    public readonly float ReleaseTime;
    public readonly float AttackShape;
    public readonly float DecayShape;
    public readonly float ReleaseShape;
    public readonly float? FinalValue;

    public ADSRModulatorNode(BinaryReader Ar)
    {
        InitialValue = Ar.ReadSingle();
        PeakValue = Ar.ReadSingle();
        SustainValue = Ar.ReadSingle();
        AttackTime = Ar.ReadSingle();
        HoldTime = Ar.ReadSingle();
        DecayTime = Ar.ReadSingle();
        ReleaseTime = Ar.ReadSingle();
        AttackShape = Ar.ReadSingle();
        DecayShape = Ar.ReadSingle();
        ReleaseShape = Ar.ReadSingle();

        if (FModReader.Version >= 0x74)
        {
            FinalValue = Ar.ReadSingle();
        }
    }
}
