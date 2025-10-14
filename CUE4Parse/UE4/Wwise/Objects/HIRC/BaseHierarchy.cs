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
    public readonly AkFxChunk[]? FxChunks;
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
            FxChunks = Ar.ReadArray(Ar.Read<byte>(), () => new AkFxChunk(Ar));
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
        writer.WritePropertyName(nameof(OverrideFx));
        writer.WriteValue(OverrideFx);

        writer.WritePropertyName(nameof(FxParams));
        serializer.Serialize(writer, FxParams);

        writer.WritePropertyName(nameof(OverrideParentMetadataFlag));
        writer.WriteValue(OverrideParentMetadataFlag != 0);

        writer.WritePropertyName(nameof(FxChunks));
        serializer.Serialize(writer, FxChunks);

        writer.WritePropertyName(nameof(OverrideAttachmentParams));
        writer.WriteValue(OverrideAttachmentParams);

        if (OverrideBusId != 0)
        {
            writer.WritePropertyName(nameof(OverrideBusId));
            writer.WriteValue(OverrideBusId);
        }

        if (DirectParentId != 0)
        {
            writer.WritePropertyName(nameof(DirectParentId));
            writer.WriteValue(DirectParentId);
        }

        writer.WritePropertyName(nameof(Priority));
        writer.WriteValue(Priority != 0);

        writer.WritePropertyName(nameof(PriorityOverrideParent));
        writer.WriteValue(PriorityOverrideParent != 0);

        writer.WritePropertyName(nameof(PriorityApplyDistFactor));
        writer.WriteValue(PriorityApplyDistFactor != 0);

        writer.WritePropertyName(nameof(DistOffset));
        writer.WriteValue(DistOffset);

        writer.WritePropertyName(nameof(MidiBehaviorFlags));
        writer.WriteValue(MidiBehaviorFlags.ToString());

        writer.WritePropertyName(nameof(Props));
        serializer.Serialize(writer, Props);

        writer.WritePropertyName(nameof(PropRanges));
        serializer.Serialize(writer, PropRanges);

        writer.WritePropertyName(nameof(PositioningParams));
        serializer.Serialize(writer, PositioningParams);

        writer.WritePropertyName(nameof(AuxParams));
        serializer.Serialize(writer, AuxParams);

        writer.WritePropertyName(nameof(AdvSettingsParams));
        writer.WriteValue(AdvSettingsParams.ToString());

        writer.WritePropertyName(nameof(VirtualQueueBehavior));
        writer.WriteValue(VirtualQueueBehavior.ToString());

        writer.WritePropertyName(nameof(MaxNumInstance));
        writer.WriteValue(MaxNumInstance);

        writer.WritePropertyName(nameof(BelowThresholdBehavior));
        writer.WriteValue(BelowThresholdBehavior.ToString());

        writer.WritePropertyName(nameof(HdrEnvelopeFlags));
        writer.WriteValue(HdrEnvelopeFlags.ToString());

        writer.WritePropertyName(nameof(StateGroups));
        serializer.Serialize(writer, StateGroups);

        writer.WritePropertyName(nameof(RtpcList));
        serializer.Serialize(writer, RtpcList);
    }
}
