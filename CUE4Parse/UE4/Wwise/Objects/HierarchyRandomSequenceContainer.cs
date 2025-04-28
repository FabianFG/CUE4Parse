using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyRandomSequenceContainer : AbstractHierarchy
    {
        public AkFXParams FXChain { get; private set; }

        public readonly byte OverrideParentMetadataFlag;
        public readonly byte NumFXMetadataFlag;

        public readonly uint OverrideBusId;
        public readonly uint DirectParentID;
        public readonly EPriorityMidi PriorityMidi;

        public List<AkProp> Props { get; private set; }
        public List<AkPropRange> PropRanges { get; private set; }

        public AkPositioningParams PositioningParams { get; private set; }

        public readonly ERandomSequence SequenceFlags;
        public readonly EBitsPositioning BitsPositioning;

        public readonly EAuxParams AuxParams;
        public List<uint> AuxIds { get; set; } = [];
        public readonly uint ReflectionsAuxBus;

        public readonly EAdvSettings AdvSettingsParams;
        public readonly byte VirtualQueueBehavior;
        public readonly ushort MaxNumInstance;
        public readonly byte BelowThresholdBehavior;
        public readonly byte HdrEnvelopeFlags;

        public List<AkStateGroup> StateGroups { get; private set; }
        public List<AkRTPC> RTPCs { get; private set; }

        public readonly ushort LoopCount;
        public readonly ushort LoopModMin;
        public readonly ushort LoopModMax;
        public readonly float TransitionTime;
        public readonly float TransitionTimeModMin;
        public readonly float TransitionTimeModMax;
        public readonly ushort AvoidRepeatCount;
        public readonly byte TransitionMode;
        public readonly byte RandomMode;
        public readonly byte Mode;

        public readonly uint[] ChildIDs;
        //public readonly AkPlaylistItem[] PlayList;

        public HierarchyRandomSequenceContainer(FArchive Ar) : base(Ar)
        {
            FXChain = Ar.ReadFXChain();

            OverrideParentMetadataFlag = Ar.Read<byte>();
            NumFXMetadataFlag = Ar.Read<byte>();

            OverrideBusId = Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

            PriorityMidi = Ar.Read<EPriorityMidi>();

            Props = Ar.ReadProps();
            PropRanges = Ar.ReadPropRanges();

            PositioningParams = Ar.ReadPositioning();

            AuxParams = Ar.Read<EAuxParams>();
            if (AuxParams.HasFlag(EAuxParams.HasAux))
                for (int i = 0; i < 4; i++)
                    AuxIds.Add(Ar.Read<uint>());
            ReflectionsAuxBus = Ar.Read<uint>();

            AdvSettingsParams = Ar.Read<EAdvSettings>();
            VirtualQueueBehavior = Ar.Read<byte>();
            MaxNumInstance = Ar.Read<ushort>();
            BelowThresholdBehavior = Ar.Read<byte>();
            HdrEnvelopeFlags = Ar.Read<byte>();

            StateGroups = Ar.ReadStateChunk();
            RTPCs= Ar.ReadRTPCList();

            LoopCount = Ar.Read<ushort>();
            LoopModMin = Ar.Read<ushort>();
            LoopModMax = Ar.Read<ushort>();
            TransitionTime = Ar.Read<float>();
            TransitionTimeModMin = Ar.Read<float>();
            TransitionTimeModMax = Ar.Read<float>();
            AvoidRepeatCount = Ar.Read<ushort>();
            TransitionMode = Ar.Read<byte>();
            RandomMode = Ar.Read<byte>();
            Mode = Ar.Read<byte>();
            SequenceFlags = Ar.Read<ERandomSequence>();

            var numChildren = Ar.Read<uint>();
            ChildIDs = new uint[numChildren];
            for (uint i = 0; i < numChildren; i++)
                ChildIDs[i] = Ar.Read<uint>();

            //var listCount = Ar.Read<ushort>();                   // ulPlayListItem
            //int itemCount = Ar.Read7BitEncodedInt();             // pItems list header
            //PlayList = new AkPlaylistItem[itemCount];
            //for (int i = 0; i < itemCount; i++)
            //{
            //    PlayList[i].PlayID = Ar.Read<uint>();
            //    PlayList[i].Weight = Ar.Read<int>();
            //}
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

            writer.WritePropertyName("PriorityMidi");
            writer.WriteValue(PriorityMidi.ToString());

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

            writer.WritePropertyName("LoopCount");
            writer.WriteValue(LoopCount);

            writer.WritePropertyName("LoopModMin");
            writer.WriteValue(LoopModMin);

            writer.WritePropertyName("LoopModMax");
            writer.WriteValue(LoopModMax);

            writer.WritePropertyName("TransitionTime");
            writer.WriteValue(TransitionTime);

            writer.WritePropertyName("TransitionTimeModMin");
            writer.WriteValue(TransitionTimeModMin);

            writer.WritePropertyName("TransitionTimeModMax");
            writer.WriteValue(TransitionTimeModMax);

            writer.WritePropertyName("AvoidRepeatCount");
            writer.WriteValue(AvoidRepeatCount);

            writer.WritePropertyName("TransitionMode");
            writer.WriteValue(TransitionMode);

            writer.WritePropertyName("RandomMode");
            writer.WriteValue(RandomMode);

            writer.WritePropertyName("Mode");
            writer.WriteValue(Mode);

            writer.WritePropertyName("SequenceFlags");
            writer.WriteValue(SequenceFlags.ToString());

            writer.WritePropertyName("ChildIDs");
            serializer.Serialize(writer, ChildIDs);

            //writer.WritePropertyName("PlayList");
            //writer.WriteStartArray();
            //foreach (var p in PlayList)
            //{
            //    writer.WriteStartObject();
            //    writer.WritePropertyName("PlayID");
            //    writer.WriteValue(p.PlayID);
            //    writer.WritePropertyName("Weight");
            //    writer.WriteValue(p.Weight);
            //    writer.WriteEndObject();
            //}
            //writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
