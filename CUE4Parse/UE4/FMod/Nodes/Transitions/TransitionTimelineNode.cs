using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes.Transitions;

public class TransitionTimelineNode
{
    public readonly uint Length;
    public readonly FTriggerBox[] TimeLockedTriggerBoxes = [];
    public readonly FTriggerBox[] TriggeredTriggerBoxes = [];
    public readonly uint LeadInLength;
    public readonly uint LeadOutLength;
    public readonly FFadeCurve[] LeadInCurves = [];
    public readonly FFadeCurve[] LeadOutCurves = [];
    public readonly FModGuid CurveMappingGuid;
    public readonly FControllerOverride[] FadeOverrides = [];

    public TransitionTimelineNode(BinaryReader Ar)
    {
        Length = Ar.ReadUInt32();
        FadeOverrides = FModReader.ReadElemListImp<FControllerOverride>(Ar);
        TimeLockedTriggerBoxes = FModReader.ReadElemListImp<FTriggerBox>(Ar);
        TriggeredTriggerBoxes = FModReader.ReadElemListImp<FTriggerBox>(Ar);

        LeadInLength = Length;

        if (FModReader.Version < 0x3E) return;

        LeadInLength = Ar.ReadUInt32();
        LeadOutLength = Ar.ReadUInt32();
        LeadInCurves = FModReader.ReadElemListImp<FFadeCurve>(Ar);
        LeadOutCurves = FModReader.ReadElemListImp<FFadeCurve>(Ar);
        CurveMappingGuid = new FModGuid(Ar);

        if (FModReader.Version >= 0x7E)
        {
            FadeOverrides = FModReader.ReadElemListImp<FControllerOverride>(Ar);
        }
    }

}
