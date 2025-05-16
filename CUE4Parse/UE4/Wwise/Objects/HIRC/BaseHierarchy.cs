using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchy : AbstractHierarchy
{
    public bool OverrideFX { get; set; }
    public AkFXParams FXParams { get; protected set; }
    public byte OverrideParentMetadataFlag { get; protected set; }
    public byte NumFXMetadataFlag { get; protected set; }
    public byte OverrideAttachmentParams { get; protected set; }
    public uint OverrideBusId { get; protected set; }
    public uint DirectParentId { get; protected set; }
    public byte Priority { get; protected set; }
    public byte PriorityOverrideParent { get; protected set; }
    public byte PriorityApplyDistFactor { get; protected set; }
    public sbyte DistOffset { get; protected set; }
    public EMidiBehaviorFlags MidiBehaviorFlags { get; protected set; }
    public List<AkProp> Props { get; protected set; }
    public List<AkPropRange> PropRanges { get; protected set; }
    public AkPositioningParams PositioningParams { get; protected set; }
    public AkAuxParams? AuxParams { get; protected set; }
    public EAdvSettings AdvSettingsParams { get; protected set; }
    public EVirtualQueueBehavior VirtualQueueBehavior { get; protected set; }
    public ushort MaxNumInstance { get; protected set; }
    public EBelowThresholdBehavior BelowThresholdBehavior { get; protected set; }
    public EHdrEnvelopeFlags HdrEnvelopeFlags { get; protected set; }
    public List<AkStateGroup> StateGroups { get; protected set; }
    public List<AkRTPC> RTPCs { get; protected set; }

    public BaseHierarchy(FArchive Ar) : base(Ar)
    {
        OverrideFX = Ar.Read<byte>() != 0;
        FXParams = new AkFXParams(Ar);

        if (WwiseVersions.WwiseVersion > 136)
        {
            SetInitialMetadataParams(Ar);
        }

        if (WwiseVersions.WwiseVersion > 89 && WwiseVersions.WwiseVersion <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>();
        }

        OverrideBusId = Ar.Read<uint>();
        DirectParentId = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion <= 56)
        {
            Priority = Ar.Read<byte>();
            PriorityOverrideParent = Ar.Read<byte>();
            PriorityApplyDistFactor = Ar.Read<byte>();
            DistOffset = Ar.Read<sbyte>();
        }
        else if (WwiseVersions.WwiseVersion <= 89)
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

        if (WwiseVersions.WwiseVersion <= 122)
        {
            // TODO: implement legacy positioning params
            PositioningParams = new AkPositioningParams(Ar);
        }
        else
        {
            PositioningParams = new AkPositioningParams(Ar);
        }

        if (WwiseVersions.WwiseVersion > 65)
        {
            AuxParams = new AkAuxParams(Ar);
        }

        SetAdvSettingsParams(Ar);

        if (WwiseVersions.WwiseVersion <= 52)
        {
            // TODO: implement legacy state handling
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else if (WwiseVersions.WwiseVersion <= 122)
        {
            // TODO: implement legacy state handling
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }

        RTPCs = AkRTPC.ReadMultiple(Ar);
    }

    private void SetInitialMetadataParams(FArchive Ar)
    {
        OverrideParentMetadataFlag = Ar.Read<byte>();
        NumFXMetadataFlag = Ar.Read<byte>();
    }

    private void SetAdvSettingsParams(FArchive Ar)
    {
        AdvSettingsParams = Ar.Read<EAdvSettings>();
        VirtualQueueBehavior = Ar.Read<EVirtualQueueBehavior>();
        MaxNumInstance = Ar.Read<ushort>();
        BelowThresholdBehavior = Ar.Read<EBelowThresholdBehavior>();
        HdrEnvelopeFlags = Ar.Read<EHdrEnvelopeFlags>();
    }

    // WriteStartEndObjects are handled by derived classes!
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("OverrideFX");
        writer.WriteValue(OverrideFX);

        writer.WritePropertyName("FXParams");
        serializer.Serialize(writer, FXParams);

        writer.WritePropertyName("OverrideParentMetadataFlag");
        writer.WriteValue(OverrideParentMetadataFlag != 0);

        writer.WritePropertyName("NumFXMetadataFlag");
        writer.WriteValue(NumFXMetadataFlag);

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

        writer.WritePropertyName("RTPCs");
        serializer.Serialize(writer, RTPCs);
    }
}
