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
    public readonly AkAdvSettingsParams AdvSettingsParams;
    public readonly EAkVirtualQueueBehavior VirtualQueueBehavior;
    public readonly ushort MaxNumInstance;
    public readonly EAkBelowThresholdBehavior BelowThresholdBehavior;
    public readonly EHdrEnvelopeFlags HdrEnvelopeFlags;
    public readonly AkStateGroup[] StateGroups;
    public readonly AkRtpc[] RtpcList;
    public readonly AkFeedbackInfo? FeedbackInfo;

    // CAkParameterNodeBase::SetNodeBaseParams
    public BaseHierarchy(FWwiseArchive Ar) : base()
    {
        OverrideFx = Ar.ReadBool();
        FxParams = new AkFxParams(Ar);

        if (Ar.Version > 136)
        {
            OverrideParentMetadataFlag = Ar.ReadBool();
            FxChunks = Ar.ReadArray(Ar.Read<byte>(), () => new AkFxChunk(Ar));
        }

        if (Ar.Version > 89 && Ar.Version <= 145)
        {
            OverrideAttachmentParams = Ar.ReadBool();
        }

        OverrideBusId = Ar.Read<uint>();
        DirectParentId = Ar.Read<uint>();

        switch (Ar.Version)
        {
            case <= 56:
                Priority = Ar.ReadBool();
                PriorityOverrideParent = Ar.ReadBool();
                PriorityApplyDistFactor = Ar.ReadBool();
                DistOffset = Ar.Read<sbyte>();
                break;
            case <= 89:
                PriorityOverrideParent = Ar.ReadBool();
                PriorityApplyDistFactor = Ar.ReadBool();
                break;
            default:
                MidiBehaviorFlags = Ar.Read<EMidiBehaviorFlags>();
                PriorityOverrideParent = (byte) (MidiBehaviorFlags == EMidiBehaviorFlags.PriorityOverrideParent ? 1 : 0) != 0;
                PriorityApplyDistFactor = (byte) (MidiBehaviorFlags == EMidiBehaviorFlags.PriorityApplyDistFactor ? 1 : 0) != 0;
                break;
        }

        PropBundle = new AkPropBundle(Ar);

        PositioningParams = new AkPositioningParams(Ar);

        if (Ar.Version > 65)
        {
            AuxParams = new AkAuxParams(Ar);
        }

        AdvSettingsParams = new AkAdvSettingsParams(Ar);

        if (Ar.Version <= 52)
        {
            // TODO: State chunk inlined
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else if (Ar.Version <= 122)
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else
        {
            StateGroups = new AkStateAwareChunk(Ar).Groups;
        }

        RtpcList = AkRtpc.ReadArray(Ar);

        if (Ar.Version <= 126 && Ar.HasFeedback)
        {
            FeedbackInfo = new AkFeedbackInfo(Ar);
        }
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

        if (WwiseConverter.WwiseVersion.Value > 65)
        {
            writer.WritePropertyName(nameof(AuxParams));
            serializer.Serialize(writer, AuxParams);
        }

        writer.WritePropertyName(nameof(AdvSettingsParams));
        serializer.Serialize(writer, AdvSettingsParams);

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

        if (WwiseConverter.WwiseVersion.Value <= 126)
        {
            writer.WritePropertyName(nameof(FeedbackInfo));
            serializer.Serialize(writer, FeedbackInfo);
        }
    }
}
