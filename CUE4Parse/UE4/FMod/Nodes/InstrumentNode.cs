using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class InstrumentNode
{
    public readonly FModGuid TimelineGuid;
    public readonly float Volume;
    public readonly float Pitch;
    public readonly int LoopCount;
    public readonly int Flags;
    public readonly float Offset3DDistance;
    public readonly float TriggerChancePercent;
    public readonly FTriggerDelay TriggerDelay;
    public readonly FQuantization Quantization;
    public readonly FModGuid ControlParameterGuid;
    public readonly float AutoPitchReference;
    public readonly float InitialSeekPosition;
    public readonly int MaximumPolyphony;
    //public readonly int PolyphonyLimitBehavior;
    //public readonly uint LeftTrimOffset;
    //public readonly float InitialSeekPercent;
    //public readonly float AutoPitchAtMinimum;
    //public readonly FEvaluatorList Evaluators;

    public InstrumentNode(BinaryReader Ar)
    {
        TimelineGuid = new FModGuid(Ar);
        Volume = Ar.ReadSingle();
        Pitch = Ar.ReadSingle();
        LoopCount = Ar.ReadInt32();

        if (FModReader.Version >= 0x82)
        {
            Flags = Ar.ReadInt32();
        }
        else
        {
            Flags = Ar.ReadByte();
        }

        Offset3DDistance = Ar.ReadSingle();
        TriggerChancePercent = Ar.ReadSingle();
        TriggerDelay = new FTriggerDelay(Ar);
        Quantization = new FQuantization(Ar);
        ControlParameterGuid = new FModGuid(Ar);
        AutoPitchReference = Ar.ReadSingle();
        InitialSeekPosition = Ar.ReadSingle();
        MaximumPolyphony = Ar.ReadInt32();

        // TODO: more to read

        //if (FModReader.Version >= 0x35)
        //{
        //    PolyphonyLimitBehavior = Ar.ReadInt32();
        //}

        //if (FModReader.Version >= 0x47)
        //{
        //    LeftTrimOffset = Ar.ReadUInt32();
        //}

        //if (FModReader.Version >= 0x48)
        //{
        //    InitialSeekPercent = Ar.ReadSingle();
        //}

        //if (FModReader.Version >= 0x50)
        //{
        //    AutoPitchAtMinimum = Ar.ReadSingle();
        //}

        //if (FModReader.Version >= 0x82)
        //{
        //    Evaluators = new FEvaluatorList(Ar);
        //}
    }
}
