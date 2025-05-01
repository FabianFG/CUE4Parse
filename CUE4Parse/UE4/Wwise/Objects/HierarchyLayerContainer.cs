using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyLayerContainer : AbstractHierarchy
    {
        public AkFXParams FXChain { get; private set; }

        public readonly byte OverrideParentMetadataFlag;
        public readonly byte NumFXMetadataFlag;

        public readonly uint OverrideBusId;
        public readonly uint DirectParentID;

        public readonly EPriorityMidi MidiFlags;

        public List<AkProp> Props { get; private set; }
        public List<AkPropRange> PropRanges { get; private set; }

        public AkPositioningParams PositioningParams { get; private set; }

        public readonly EAuxParams AuxParams;
        public List<uint> AuxIds { get; set; } = [];
        public readonly uint ReflectionsAuxBus;

        public readonly EAdvSettings AdvFlags;
        public readonly byte VirtualQueueBehavior;
        public readonly ushort MaxNumInstances;
        public readonly byte BelowThresholdBehavior;
        public readonly byte HdrEnvelopeFlags;

        public List<AkStateGroup> StateGroups { get; private set; }
        public List<AkRTPC> RTPCs { get; private set; }

        public readonly uint[] ChildIDs;

        public HierarchyLayerContainer(FArchive Ar) : base(Ar)
        {
            FXChain = new AkFXParams(Ar);

            OverrideParentMetadataFlag = Ar.Read<byte>();
            NumFXMetadataFlag = Ar.Read<byte>();
            if (WwiseVersions.WwiseVersion <= 145)
                Ar.Read<byte>();

            OverrideBusId = Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

            MidiFlags = Ar.Read<EPriorityMidi>();

            AkPropBundle propBundle = new(Ar);
            Props = propBundle.Props;
            PropRanges = propBundle.PropRanges;

            PositioningParams = new AkPositioningParams(Ar);

            AuxParams = Ar.Read<EAuxParams>();
            if (AuxParams.HasFlag(EAuxParams.HasAux))
                for (int i = 0; i < 4; i++)
                    AuxIds.Add(Ar.Read<uint>());
            ReflectionsAuxBus = Ar.Read<uint>();

            AdvFlags = Ar.Read<EAdvSettings>();
            VirtualQueueBehavior = Ar.Read<byte>();
            MaxNumInstances = Ar.Read<ushort>();
            BelowThresholdBehavior = Ar.Read<byte>();
            HdrEnvelopeFlags = Ar.Read<byte>();

            StateGroups = new AkStateChunk(Ar).Groups;
            RTPCs = new AkRTPCList(Ar);

            var numChildren = Ar.Read<uint>();
            ChildIDs = new uint[numChildren];
            for (var i = 0; i < numChildren; i++)
                ChildIDs[i] = Ar.Read<uint>();

            // Layers count and IDs
            //var numLayers = Ar.Read<uint>();
            //for (var i = 0; i < numLayers; i++)
            //    Ar.Read<uint>();

            // var continuousValidation = Ar.Read<byte>();
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

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

            writer.WritePropertyName("MidiFlags");
            writer.WriteValue(MidiFlags.ToString());

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

            writer.WritePropertyName("AdvFlags");
            writer.WriteValue(AdvFlags.ToString());

            writer.WritePropertyName("VirtualQueueBehavior");
            writer.WriteValue(VirtualQueueBehavior);

            writer.WritePropertyName("MaxNumInstances");
            writer.WriteValue(MaxNumInstances);

            writer.WritePropertyName("BelowThresholdBehavior");
            writer.WriteValue(BelowThresholdBehavior);

            writer.WritePropertyName("HdrEnvelopeFlags");
            writer.WriteValue(HdrEnvelopeFlags);

            writer.WritePropertyName("StateGroups");
            serializer.Serialize(writer, StateGroups);

            writer.WritePropertyName("RTPCs");
            serializer.Serialize(writer, RTPCs);

            writer.WritePropertyName("ChildIDs");
            serializer.Serialize(writer, ChildIDs);

            writer.WriteEndObject();
        }
    }
}
