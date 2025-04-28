using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{

    public struct AkPlaylistItem
    {
        public uint PlayID;
        public int Weight;
    }

    public class HierarchyRandomSequenceContainer : AbstractHierarchy
    {
        public readonly uint OverrideBusId;
        public readonly uint DirectParentID;
        public readonly EPriorityMidi PriorityMidi;

        public List<AkProp> Props { get; private set; }
        public List<AkPropRange> PropRanges { get; private set; }

        public readonly ERandomSequence SequenceFlags;
        public readonly EBitsPositioning BitsPositioning;

        public readonly EAuxParams AuxParams;
        public List<uint> AuxIds { get; set; } = [];
        public readonly uint ReflectionsAuxBus;

        public readonly EAdvSettings AdvSettingsParams;
        public readonly byte VirtualQueueBehavior;
        public readonly ushort MaxNumInstance;
        public readonly byte BelowThresholdBehavior;

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
        public readonly AkPlaylistItem[] PlayList;

        public HierarchyRandomSequenceContainer(FArchive Ar) : base(Ar)
        {
            byte bOverrideFx = Ar.Read<byte>();
            byte uNumFx = Ar.Read<byte>();
            if (bOverrideFx != 0 && uNumFx != 0)
            {
                byte bBypassAll = Ar.Read<byte>(); // Only when overriding FX

                for (int i = 0; i < uNumFx; i++)
                {
                    Ar.Read<byte>(); // uFXIndex
                    Ar.Read<uint>(); // fxID
                    Ar.Read<byte>(); // bitVector
                }
            }

            Ar.Read<byte>(); // bIsOverrideParentMetadata
            Ar.Read<byte>(); // uNumFx (metadata)

            OverrideBusId = Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

            PriorityMidi = Ar.Read<EPriorityMidi>();

            Props = Ar.ReadProps();
            PropRanges = Ar.ReadPropRanges();

            BitsPositioning = Ar.Read<EBitsPositioning>();
            if (BitsPositioning.IsEmitter() || BitsPositioning.HasFlag(EBitsPositioning.HasListenerRelativeRouting))
                Ar.Read<byte>(); // 3D flags

            if (BitsPositioning.HasFlag(EBitsPositioning.PositioningInfoOverrideParent) && BitsPositioning.IsEmitter())
            {
                byte ePathMode = Ar.Read<byte>();
                int transitionTime = Ar.Read<int>();

                uint numVertices = Ar.Read<uint>();
                for (int i = 0; i < numVertices; i++)
                {
                    Ar.Read<float>(); // Vertex.X
                    Ar.Read<float>(); // Vertex.Y
                    Ar.Read<float>(); // Vertex.Z
                    Ar.Read<int>();   // Duration
                }

                uint numPlaylistItems = Ar.Read<uint>();
                for (int i = 0; i < numPlaylistItems; i++)
                {
                    Ar.Read<uint>(); // ulVerticesOffset
                    Ar.Read<uint>(); // iNumVertices
                }

                for (int i = 0; i < numPlaylistItems; i++)
                {
                    Ar.Read<float>(); // fXRange
                    Ar.Read<float>(); // fYRange
                    Ar.Read<float>(); // fZRange
                }
            }

            AuxParams = Ar.Read<EAuxParams>();
            if (AuxParams.HasFlag(EAuxParams.HasAux))
                for (int i = 0; i < 4; i++)
                    AuxIds.Add(Ar.Read<uint>());
            ReflectionsAuxBus = Ar.Read<uint>();

            AdvSettingsParams = Ar.Read<EAdvSettings>();
            VirtualQueueBehavior = Ar.Read<byte>();
            MaxNumInstance = Ar.Read<ushort>();
            BelowThresholdBehavior = Ar.Read<byte>();
            Ar.Read<byte>();   // hdrEnvelopeFlags

            StateGroups = Ar.ReadStateChunk();
            RTPCs= Ar.ReadRTPCList();

            LoopCount = Ar.Read<ushort>(); // sLoopCount
            LoopModMin = Ar.Read<ushort>(); // sLoopModMin
            LoopModMax = Ar.Read<ushort>(); // sLoopModMax
            TransitionTime = Ar.Read<float>();  // fTransitionTime
            TransitionTimeModMin = Ar.Read<float>();  // fTransitionTimeModMin
            TransitionTimeModMax = Ar.Read<float>();  // fTransitionTimeModMax
            AvoidRepeatCount = Ar.Read<ushort>(); // wAvoidRepeatCount
            TransitionMode = Ar.Read<byte>();   // eTransitionMode
            RandomMode = Ar.Read<byte>();   // eRandomMode
            Mode = Ar.Read<byte>();   // eMode
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
            writer.WritePropertyName("DirectParentID");
            writer.WriteValue(DirectParentID);
            writer.WritePropertyName("ChildIDs");
            writer.WriteStartArray();
            foreach (var c in ChildIDs)
                writer.WriteValue(c);
            writer.WriteEndArray();
            //writer.WritePropertyName("PlayList");
            //writer.WriteStartArray();
            //foreach (var p in PlayList)
            //{ writer.WriteStartObject(); writer.WritePropertyName("PlayID"); writer.WriteValue(p.PlayID); writer.WritePropertyName("Weight"); writer.WriteValue(p.Weight); writer.WriteEndObject(); }
            //writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
