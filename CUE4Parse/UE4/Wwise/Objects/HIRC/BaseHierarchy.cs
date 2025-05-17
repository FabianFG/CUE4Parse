using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchy : AbstractHierarchy
{
    public bool OverrideFX { get; set; }
    public AkFXParams FXParams { get; private set; }
    public byte OverrideParentMetadataFlag { get; private set; }
    public byte NumFXMetadataFlag { get; private set; }
    public byte OverrideAttachmentParams { get; private set; }
    public uint OverrideBusId { get; private set; }
    public uint DirectParentId { get; private set; }
    public byte Priority { get; private set; }
    public byte PriorityOverrideParent { get; private set; }
    public byte PriorityApplyDistFactor { get; private set; }
    public sbyte DistOffset { get; private set; }
    public EMidiBehaviorFlags MidiBehaviorFlags { get; private set; }
    public List<AkProp> Props { get; private set; }
    public List<AkPropRange> PropRanges { get; private set; }
    public AkPositioningParams PositioningParams { get; private set; }
    public AkAuxParams? AuxParams { get; private set; }
    public EAdvSettings AdvSettingsParams { get; private set; }
    public EVirtualQueueBehavior VirtualQueueBehavior { get; private set; }
    public ushort MaxNumInstance { get; private set; }
    public EBelowThresholdBehavior BelowThresholdBehavior { get; private set; }
    public EHdrEnvelopeFlags HdrEnvelopeFlags { get; private set; }
    public List<AkStateGroup> StateGroups { get; private set; }
    public List<AkRTPC> RTPCs { get; private set; }

    public BaseHierarchy(FArchive Ar) : base(Ar)
    {
        OverrideFX = Ar.Read<byte>() != 0;
        FXParams = new AkFXParams(Ar);

        if (WwiseVersions.Version > 136)
        {
            SetInitialMetadataParams(Ar);
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

        if (WwiseVersions.Version <= 122)
        {
            // TODO: implement legacy positioning params
            PositioningParams = new AkPositioningParams(Ar);
        }
        else
        {
            PositioningParams = new AkPositioningParams(Ar);
        }

        if (WwiseVersions.Version > 65)
        {
            AuxParams = new AkAuxParams(Ar);
        }

        SetAdvSettingsParams(Ar);

        if (WwiseVersions.Version <= 52)
        {
            // TODO: implement legacy state handling
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else if (WwiseVersions.Version <= 122)
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
