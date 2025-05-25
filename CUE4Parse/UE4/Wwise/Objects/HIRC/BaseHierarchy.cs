using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchy : AbstractHierarchy
{
    public readonly bool OverrideFx;
    public readonly AkFxParams FxParams;
    public readonly byte OverrideParentMetadataFlag;
    public readonly byte NumFxMetadataFlag;
    public readonly byte OverrideAttachmentParams;
    public readonly uint OverrideBusId;
    public readonly uint DirectParentId;
    public readonly byte Priority;
    public readonly byte PriorityOverrideParent;
    public readonly byte PriorityApplyDistFactor;
    public readonly sbyte DistOffset;
    public readonly EMidiBehaviorFlags MidiBehaviorFlags;
    public readonly List<AkProp> Props;
    public readonly List<AkPropRange> PropRanges;
    public readonly AkPositioningParams PositioningParams;
    public readonly AkAuxParams? AuxParams;
    public readonly EAdvSettings AdvSettingsParams;
    public readonly EVirtualQueueBehavior VirtualQueueBehavior;
    public readonly ushort MaxNumInstance;
    public readonly EBelowThresholdBehavior BelowThresholdBehavior;
    public readonly EHdrEnvelopeFlags HdrEnvelopeFlags;
    public readonly List<AkStateGroup> StateGroups;
    public readonly List<AkRtpc> RtpcList;

    public BaseHierarchy(FArchive Ar) : base(Ar)
    {
        OverrideFx = Ar.Read<byte>() != 0;
        FxParams = new AkFxParams(Ar);

        if (WwiseVersions.Version > 136)
        {
            OverrideParentMetadataFlag = Ar.Read<byte>();
            NumFxMetadataFlag = Ar.Read<byte>();
        }

        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>();
        }

        OverrideBusId = Ar.Read<uint>();
        DirectParentId = Ar.Read<uint>();

        if (WwiseVersions.Version <= 56)
        {
            Priority = Ar.Read<byte>();
            PriorityOverrideParent = Ar.Read<byte>();
            PriorityApplyDistFactor = Ar.Read<byte>();
            DistOffset = Ar.Read<sbyte>();
        }
        else if (WwiseVersions.Version <= 89)
        {
            PriorityOverrideParent = Ar.Read<byte>();
            PriorityApplyDistFactor = Ar.Read<byte>();
        }
        else
        {
            MidiBehaviorFlags = Ar.Read<EMidiBehaviorFlags>();

            PriorityOverrideParent = (byte) (MidiBehaviorFlags == EMidiBehaviorFlags.PriorityOverrideParent ? 1 : 0);
            PriorityApplyDistFactor = (byte) (MidiBehaviorFlags == EMidiBehaviorFlags.PriorityApplyDistFactor ? 1 : 0);
        }

        AkPropBundle propBundle = new(Ar);
        Props = propBundle.Props;
        PropRanges = propBundle.PropRanges;

        PositioningParams = new AkPositioningParams(Ar);

        if (WwiseVersions.Version > 65)
        {
            AuxParams = new AkAuxParams(Ar);
        }

        AdvSettingsParams = Ar.Read<EAdvSettings>();
        VirtualQueueBehavior = Ar.Read<EVirtualQueueBehavior>();
        MaxNumInstance = Ar.Read<ushort>();
        BelowThresholdBehavior = Ar.Read<EBelowThresholdBehavior>();
        HdrEnvelopeFlags = Ar.Read<EHdrEnvelopeFlags>();

        if (WwiseVersions.Version <= 52)
        {
            // TODO: State chunk inlined
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else if (WwiseVersions.Version <= 122)
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else
        {
            StateGroups = new AkStateAwareChunk(Ar).Groups;
        }

        RtpcList = AkRtpc.ReadMultiple(Ar);
    }

    // WriteStartEndObjects are handled by derived classes!
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("OverrideFx");
        writer.WriteValue(OverrideFx);

        writer.WritePropertyName("FxParams");
        serializer.Serialize(writer, FxParams);

        writer.WritePropertyName("OverrideParentMetadataFlag");
        writer.WriteValue(OverrideParentMetadataFlag != 0);

        writer.WritePropertyName("NumFxMetadataFlag");
        writer.WriteValue(NumFxMetadataFlag);

        writer.WritePropertyName("OverrideAttachmentParams");
        writer.WriteValue(OverrideAttachmentParams);

        if (OverrideBusId != 0)
        {
            writer.WritePropertyName("OverrideBusId");
            writer.WriteValue(OverrideBusId);
        }

        if (DirectParentId != 0)
        {
            writer.WritePropertyName("DirectParentId");
            writer.WriteValue(DirectParentId);
        }

        writer.WritePropertyName("Priority");
        writer.WriteValue(Priority != 0);

        writer.WritePropertyName("PriorityOverrideParent");
        writer.WriteValue(PriorityOverrideParent != 0);

        writer.WritePropertyName("PriorityApplyDistFactor");
        writer.WriteValue(PriorityApplyDistFactor != 0);

        writer.WritePropertyName("DistOffset");
        writer.WriteValue(DistOffset);

        writer.WritePropertyName("MidiBehaviorFlags");
        writer.WriteValue(MidiBehaviorFlags.ToString());

        writer.WritePropertyName("Props");
        serializer.Serialize(writer, Props);

        writer.WritePropertyName("PropRanges");
        serializer.Serialize(writer, PropRanges);

        writer.WritePropertyName("PositioningParams");
        serializer.Serialize(writer, PositioningParams);

        writer.WritePropertyName("AuxParams");
        serializer.Serialize(writer, AuxParams);

        writer.WritePropertyName("AdvSettingsParams");
        writer.WriteValue(AdvSettingsParams.ToString());

        writer.WritePropertyName("VirtualQueueBehavior");
        writer.WriteValue(VirtualQueueBehavior.ToString());

        writer.WritePropertyName("MaxNumInstance");
        writer.WriteValue(MaxNumInstance);

        writer.WritePropertyName("BelowThresholdBehavior");
        writer.WriteValue(BelowThresholdBehavior.ToString());

        writer.WritePropertyName("HdrEnvelopeFlags");
        writer.WriteValue(HdrEnvelopeFlags.ToString());

        writer.WritePropertyName("StateGroups");
        serializer.Serialize(writer, StateGroups);

        writer.WritePropertyName("RtpcList");
        serializer.Serialize(writer, RtpcList);
    }
}
