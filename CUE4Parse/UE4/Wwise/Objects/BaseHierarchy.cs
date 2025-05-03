using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class BaseHierarchy : AbstractHierarchy
    {
        public AkFXParams FXChain { get; protected set; }
        public byte OverrideParentMetadataFlag { get; protected set; }
        public byte NumFXMetadataFlag { get; protected set; }
        public byte OverrideAttachmentParams { get; protected set; }
        public uint OverrideBusId { get; protected set; }
        public uint DirectParentID { get; protected set; }
        public byte Priority { get; protected set; }
        public byte PriorityOverrideParent { get; protected set; }
        public byte PriorityApplyDistFactor { get; protected set; }
        public sbyte DistOffset { get; protected set; }
        public byte ByBitVector { get; protected set; }
        public List<AkProp> Props { get; protected set; }
        public List<AkPropRange> PropRanges { get; protected set; }
        public AkPositioningParams PositioningParams { get; protected set; }
        public EAuxParams AuxParams { get; protected set; }
        public List<uint> AuxIds { get; protected set; } = new();
        public uint ReflectionsAuxBus { get; protected set; }
        public EAdvSettings AdvSettingsParams { get; protected set; }
        public byte VirtualQueueBehavior { get; protected set; }
        public ushort MaxNumInstance { get; protected set; }
        public byte BelowThresholdBehavior { get; protected set; }
        public byte HdrEnvelopeFlags { get; protected set; }
        public List<AkStateGroup> StateGroups { get; protected set; }
        public List<AkRTPC> RTPCs { get; protected set; }

        public BaseHierarchy(FArchive Ar) : base(Ar)
        {
            FXChain = new AkFXParams(Ar);

            if (WwiseVersions.WwiseVersion > 136)
            {
                SetInitialMetadataParams(Ar);
            }

            if (WwiseVersions.WwiseVersion <= 145)
            {
                OverrideAttachmentParams = Ar.Read<byte>();
            }

            OverrideBusId = Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

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
                ByBitVector = Ar.Read<byte>();

                PriorityOverrideParent = (byte) ((ByBitVector & 0b00000001) != 0 ? 1 : 0);
                PriorityApplyDistFactor = (byte) ((ByBitVector & 0b00000010) != 0 ? 1 : 0);
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
                SetAuxParams(Ar);
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

            RTPCs = new AkRTPCList(Ar);
        }

        private void SetInitialMetadataParams(FArchive Ar)
        {
            OverrideParentMetadataFlag = Ar.Read<byte>();
            NumFXMetadataFlag = Ar.Read<byte>();
        }

        private void SetAuxParams(FArchive Ar)
        {
            AuxParams = Ar.Read<EAuxParams>();
            if (AuxParams.HasFlag(EAuxParams.HasAux))
            {
                for (int i = 0; i < 4; i++)
                {
                    AuxIds.Add(Ar.Read<uint>());
                }
            }
            ReflectionsAuxBus = Ar.Read<uint>();
        }

        private void SetAdvSettingsParams(FArchive Ar)
        {
            AdvSettingsParams = Ar.Read<EAdvSettings>();
            VirtualQueueBehavior = Ar.Read<byte>();
            MaxNumInstance = Ar.Read<ushort>();
            BelowThresholdBehavior = Ar.Read<byte>();
            HdrEnvelopeFlags = Ar.Read<byte>();
        }

        // WriteStartEndObjects are handled by derived classes!
        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            //writer.WriteStartObject();

            writer.WritePropertyName("FXChain");
            serializer.Serialize(writer, FXChain);

            writer.WritePropertyName("OverrideParentMetadataFlag");
            writer.WriteValue(OverrideParentMetadataFlag);

            writer.WritePropertyName("NumFXMetadataFlag");
            writer.WriteValue(NumFXMetadataFlag);

            writer.WritePropertyName("OverrideBusId");
            writer.WriteValue(OverrideBusId);

            writer.WritePropertyName("DirectParentID");
            writer.WriteValue(DirectParentID);

            writer.WritePropertyName("Props");
            serializer.Serialize(writer, Props);

            writer.WritePropertyName("PropRanges");
            serializer.Serialize(writer, PropRanges);

            writer.WritePropertyName("PositioningParams");
            serializer.Serialize(writer, PositioningParams);

            writer.WritePropertyName("AuxParams");
            writer.WriteValue(AuxParams.ToString());

            writer.WritePropertyName("AuxIds");
            serializer.Serialize(writer, AuxIds);

            writer.WritePropertyName("ReflectionsAuxBus");
            writer.WriteValue(ReflectionsAuxBus);

            writer.WritePropertyName("AdvSettingsParams");
            writer.WriteValue(AdvSettingsParams.ToString());

            writer.WritePropertyName("VirtualQueueBehavior");
            writer.WriteValue(VirtualQueueBehavior);

            writer.WritePropertyName("MaxNumInstance");
            writer.WriteValue(MaxNumInstance);

            writer.WritePropertyName("BelowThresholdBehavior");
            writer.WriteValue(BelowThresholdBehavior);

            writer.WritePropertyName("HdrEnvelopeFlags");
            writer.WriteValue(HdrEnvelopeFlags);

            writer.WritePropertyName("StateGroups");
            serializer.Serialize(writer, StateGroups);

            writer.WritePropertyName("RTPCs");
            serializer.Serialize(writer, RTPCs);

            //writer.WriteEndObject();
        }
    }
}
