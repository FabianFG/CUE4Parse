using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class EventNode
{
    public readonly FModGuid BaseGuid;
    public readonly FModGuid SnapshotGuid;
    public readonly FModGuid TimelineGuid;
    public readonly FModGuid InputBusGuid;
    public readonly FModGuid MasterTrackGuid;

    public readonly int MaximumPolyphony;
    public readonly int Priority;
    public readonly bool PolyphonyLimitBehavior;
    public readonly int SchedulingMode;

    public readonly FModGuid[] ParameterLayouts;

    public readonly FUserPropertyFloat[] UserPropertyFloatList;
    public readonly FUserPropertyString[] UserPropertyStringList;

    public readonly float? DopplerScale;
    public readonly float? TriggerCooldown;
    public readonly uint? Flags;

    public readonly FModGuid[] NonMasterTracks = [];
    public readonly FParameterId[] ParameterIds = [];
    public readonly FModGuid[] EventTriggeredInstruments = [];

    public readonly float? MinimumDistance;
    public readonly float? MaximumDistance;

    public EventNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        SnapshotGuid = new FModGuid(Ar);
        TimelineGuid = new FModGuid(Ar);
        InputBusGuid = new FModGuid(Ar);
        MasterTrackGuid = new FModGuid(Ar);

        MaximumPolyphony = Ar.ReadInt32();
        Priority = Ar.ReadInt32();
        PolyphonyLimitBehavior = Ar.ReadByte() != 0;
        SchedulingMode = Ar.ReadInt32();

        ParameterLayouts = FModReader.ReadElemListImp<FModGuid>(Ar);

        UserPropertyFloatList = FModReader.ReadElemListImp<FUserPropertyFloat>(Ar);
        UserPropertyStringList = FModReader.ReadElemListImp<FUserPropertyString>(Ar);

        if (FModReader.Version >= 0x30) DopplerScale = Ar.ReadSingle();
        if (FModReader.Version >= 0x34) PolyphonyLimitBehavior = Ar.ReadInt32() != 0;

        if (FModReader.Version >= 0x4e) TriggerCooldown = Ar.ReadSingle();
        if (FModReader.Version >= 0x61) Flags = Ar.ReadUInt32();

        if (FModReader.Version >= 0x6b) NonMasterTracks = FModReader.ReadElemListImp<FModGuid>(Ar);
        if (FModReader.Version >= 0x76) ParameterIds = FModReader.ReadElemListImp<FParameterId>(Ar);
        if (FModReader.Version >= 0x83) EventTriggeredInstruments = FModReader.ReadElemListImp<FModGuid>(Ar);

        if (FModReader.Version >= 0x89)
        {
            MinimumDistance = Ar.ReadSingle();
            MaximumDistance = Ar.ReadSingle();
        }
    }
}
