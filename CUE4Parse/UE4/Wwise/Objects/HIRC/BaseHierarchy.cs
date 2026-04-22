using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

// CAkParameterNodeBase
public class BaseHierarchy : AbstractHierarchy
{
    public readonly bool OverrideFx;
    public readonly AkFxParams FxParams;
    public readonly bool OverrideParentMetadataFlag;
    public readonly AkFxChunk[] FxChunks = [];
    public readonly bool OverrideAttachmentParams;
    public readonly uint OverrideBusId;
    public readonly uint DirectParentId;
    public readonly bool Priority;
    public readonly bool PriorityOverrideParent;
    public readonly bool PriorityApplyDistFactor;
    public readonly sbyte DistOffset;
    public readonly EMidiBehaviorFlags MidiBehaviorFlags;
    public readonly AkPropBundle PropBundle;
    public readonly AkPositioningParams PositioningParams;
    public readonly AkAuxParams? AuxParams;
    public readonly EAdvSettings AdvSettingsParams;
    public readonly EAkVirtualQueueBehavior VirtualQueueBehavior;
    public readonly ushort MaxNumInstance;
    public readonly EAkBelowThresholdBehavior BelowThresholdBehavior;
    public readonly EHdrEnvelopeFlags HdrEnvelopeFlags;
    public readonly AkStateGroup[] StateGroups;
    public readonly AkRtpc[] RtpcList;

    // CAkParameterNodeBase::SetNodeBaseParams
    public BaseHierarchy(FArchive Ar) : base(Ar)
    {
        OverrideFx = Ar.Read<byte>() != 0;
        FxParams = new AkFxParams(Ar);

        if (WwiseVersions.Version > 136)
        {
            OverrideParentMetadataFlag = Ar.Read<byte>() != 0;
            FxChunks = Ar.ReadArray(Ar.Read<byte>(), () => new AkFxChunk(Ar));
        }

        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>() != 0;
        }

        OverrideBusId = Ar.Read<uint>();
        DirectParentId = Ar.Read<uint>();

        if (WwiseVersions.Version <= 56)
        {
            Priority = Ar.Read<byte>() != 0;
            PriorityOverrideParent = Ar.Read<byte>() != 0;
            PriorityApplyDistFactor = Ar.Read<byte>() != 0;
            DistOffset = Ar.Read<sbyte>();
        }
        else if (WwiseVersions.Version <= 89)
        {
            PriorityOverrideParent = Ar.Read<byte>() != 0;
            PriorityApplyDistFactor = Ar.Read<byte>() != 0;
        }
        else
        {
            MidiBehaviorFlags = Ar.Read<EMidiBehaviorFlags>();

            PriorityOverrideParent = (byte) (MidiBehaviorFlags == EMidiBehaviorFlags.PriorityOverrideParent ? 1 : 0) != 0;
            PriorityApplyDistFactor = (byte) (MidiBehaviorFlags == EMidiBehaviorFlags.PriorityApplyDistFactor ? 1 : 0) != 0;
        }

        PropBundle = new AkPropBundle(Ar);

        PositioningParams = new AkPositioningParams(Ar);

        if (WwiseVersions.Version > 65)
        {
            AuxParams = new AkAuxParams(Ar);
        }

        AdvSettingsParams = Ar.Read<EAdvSettings>();
        VirtualQueueBehavior = Ar.Read<EAkVirtualQueueBehavior>();
        MaxNumInstance = Ar.Read<ushort>();
        BelowThresholdBehavior = Ar.Read<EAkBelowThresholdBehavior>();
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

        RtpcList = AkRtpc.ReadArray(Ar);
    }

    // WriteStartEndObjects are handled by derived classes!
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName(nameof(OverrideFx));
        writer.WriteValue(OverrideFx);

        writer.WritePropertyName(nameof(FxParams));
        serializer.Serialize(writer, FxParams);

        writer.WritePropertyName(nameof(OverrideParentMetadataFlag));
        writer.WriteValue(OverrideParentMetadataFlag);

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
        writer.WriteValue(Priority);

        writer.WritePropertyName(nameof(PriorityOverrideParent));
        writer.WriteValue(PriorityOverrideParent);

        writer.WritePropertyName(nameof(PriorityApplyDistFactor));
        writer.WriteValue(PriorityApplyDistFactor);

        writer.WritePropertyName(nameof(DistOffset));
        writer.WriteValue(DistOffset);

        writer.WritePropertyName(nameof(MidiBehaviorFlags));
        writer.WriteValue(MidiBehaviorFlags.ToString());

        writer.WritePropertyName(nameof(PropBundle));
        serializer.Serialize(writer, PropBundle);

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
