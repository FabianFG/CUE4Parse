using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{

    public class HierarchyLayerContainer : AbstractHierarchy
    {
        public readonly uint DirectParentID;
        public readonly uint[] ChildIDs;

        public List<AkProp> Props { get; private set; }
        public List<AkPropRange> PropRanges { get; private set; }

        public readonly EAuxParams AuxParams;
        public List<uint> AuxIds { get; set; } = [];
        public readonly uint ReflectionsAuxBus;

        public List<AkStateGroup> StateGroups { get; private set; }
        public List<AkRTPC> RTPCs { get; private set; }

        public HierarchyLayerContainer(FArchive Ar) : base(Ar)
        {
            // NodeInitialFxParams
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

            // Metadata override
            Ar.Read<byte>();
            Ar.Read<byte>();

            // OverrideBusId
            Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

            var midiFlags = Ar.Read<EPriorityMidi>();

            Props = Ar.ReadProps();
            PropRanges = Ar.ReadPropRanges();

            var positioningBits = Ar.Read<EBitsPositioning>();
            if (positioningBits.IsEmitter() || positioningBits.HasFlag(EBitsPositioning.HasListenerRelativeRouting))
                Ar.Read<byte>(); // 3D flags

            if (positioningBits.HasFlag(EBitsPositioning.PositioningInfoOverrideParent) && positioningBits.IsEmitter())
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

            var advFlags = Ar.Read<EAdvSettings>();
            var virtualQueueBehavior = Ar.Read<byte>();
            var maxNumInstances = Ar.Read<ushort>();
            var belowThresholdBehavior = Ar.Read<byte>();
            var hdrEnvelopeFlags = Ar.Read<byte>();

            StateGroups = Ar.ReadStateChunk();
            RTPCs = Ar.ReadRTPCList();

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
            writer.WritePropertyName("DirectParentID");
            writer.WriteValue(DirectParentID);
            writer.WritePropertyName("ChildIDs");
            writer.WriteStartArray();
            foreach (var cid in ChildIDs)
                writer.WriteValue(cid);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
