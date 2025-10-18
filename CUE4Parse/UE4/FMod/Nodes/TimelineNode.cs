using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class TimelineNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid LegacyGuid;
    public readonly FTriggerBox[] TriggerBoxes = [];
    public readonly FTriggerBox[] TimeLockedTriggerBoxes = [];
    public readonly FSustainPoint[] SustainPoints = [];
    public readonly FTimelineNamedMarker[] TimelineNamedMarkers = [];
    public readonly FTimelineTempoMarker[] TimelineTempoMarkers = [];
    public readonly uint[] LegacyUIntArray = [];

    public TimelineNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);

        if (FModReader.Version < 0x6d)
        {
            LegacyGuid = new FModGuid(Ar);
        }

        TriggerBoxes = FModReader.ReadElemListImp<FTriggerBox>(Ar);
        TimeLockedTriggerBoxes = FModReader.ReadElemListImp<FTriggerBox>(Ar);

        if (FModReader.Version < 0x84)
        {
            LegacyUIntArray = FModReader.ReadElemListImp(Ar, Ar => Ar.ReadUInt32());
        }
        else
        {
            SustainPoints = FModReader.ReadVersionedElemListImp<FSustainPoint>(Ar);
        }

        TimelineNamedMarkers = FModReader.ReadVersionedElemListImp<FTimelineNamedMarker>(Ar);
        TimelineTempoMarkers = FModReader.ReadElemListImp<FTimelineTempoMarker>(Ar);
    }
}
