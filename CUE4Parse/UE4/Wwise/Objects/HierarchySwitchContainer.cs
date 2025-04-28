using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Wwise.Objects
{

    public class HierarchySwitchContainer : AbstractHierarchy
    {
        public readonly byte eHircType;
        public readonly uint dwSectionSize;
        public readonly uint ulID;

        public readonly byte bIsOverrideParentFX;
        public readonly byte uNumFx1;
        public readonly byte bIsOverrideParentMetadata;
        public readonly byte uNumFx2;

        public readonly uint OverrideBusId;
        public readonly uint DirectParentID;

        public readonly ESwitchContainer BitVectorSwitch;
        //public readonly bool bPriorityOverrideParent;
        //public readonly bool bPriorityApplyDistFactor;
        //public readonly bool bOverrideMidiEventsBehavior;
        //public readonly bool bOverrideMidiNoteTracking;
        //public readonly bool bEnableMidiNoteTracking;
        //public readonly bool bIsMidiBreakLoopOnNoteOff;

        public List<AkProp> Props { get; private set; }
        public List<AkPropRange> PropRanges { get; private set; }

        // Positioning
        public readonly EBitsPositioning BitsPositioning;

        public readonly EAuxParams auxParamsBitVector;
        public readonly uint reflectionsAuxBus;

        // Advanced Settings
        public readonly EAdvSettings AdvSettingsParams;
        public readonly byte eVirtualQueueBehavior;
        public readonly ushort u16MaxNumInstance;
        public readonly byte eBelowThresholdBehavior;
        public readonly byte hdrEnvelopeBitVector;

        public List<AkStateGroup> StateGroups { get; private set; }
        public List<AkRTPC> RTPCs { get; private set; }

        // StateChunk
        public readonly uint ulNumStateProps;
        public readonly uint ulNumStateGroups;

        // InitialRTPC
        public readonly ushort uNumCurves;

        // SwitchContainerGroup
        public readonly byte eGroupType;
        public readonly uint ulGroupID;
        public readonly uint ulDefaultSwitch;
        public readonly byte bIsContinuousValidation;

        // Children
        public readonly uint ulNumChilds;
        public readonly List<uint> ChildIDs = new();

        public HierarchySwitchContainer(FArchive Ar) : base(Ar)
        {
            // --- NodeInitialFxParams ---
            byte bOverrideFx = Ar.Read<byte>();
            byte uNumFx = Ar.Read<byte>();
            if (bOverrideFx != 0)
            {
                byte bBypassAll = Ar.Read<byte>(); // Only when overriding FX

                for (int i = 0; i < uNumFx; i++)
                {
                    Ar.Read<byte>(); // uFXIndex
                    Ar.Read<uint>(); // fxID
                    Ar.Read<byte>(); // bitVector
                }
            }

            // --- Metadata override ---
            Ar.Read<byte>(); // bIsOverrideParentMetadata
            Ar.Read<byte>(); // uNumFx (metadata)

            OverrideBusId = Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

            BitVectorSwitch = Ar.Read<ESwitchContainer>();

            // NodeInitialParams: first PropBundle
            Props = Ar.ReadProps();
            PropRanges = Ar.ReadPropRanges();

            // PositioningParams
            BitsPositioning = Ar.Read<EBitsPositioning>();
            var pannerType = BitsPositioning.GetPannerType();
            if (BitsPositioning.IsEmitter() || BitsPositioning.HasFlag(EBitsPositioning.HasListenerRelativeRouting))
                Ar.Read<byte>(); // read 3d flags

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

            // AuxParams
            auxParamsBitVector = Ar.Read<EAuxParams>();
            if (auxParamsBitVector.HasFlag(EAuxParams.HasAux))
                for (int i = 0; i < 4; i++)
                    Ar.Read<uint>();
            reflectionsAuxBus = Ar.Read<uint>();

            // AdvSettingsParams
            AdvSettingsParams = Ar.Read<EAdvSettings>();
            eVirtualQueueBehavior = Ar.Read<byte>();
            u16MaxNumInstance = Ar.Read<ushort>();
            eBelowThresholdBehavior = Ar.Read<byte>();
            hdrEnvelopeBitVector = Ar.Read<byte>();

            //// StateChunk
            //WwiseReader.ReadStateChunk(Ar);

            //// InitialRTPC
            //WwiseReader.ReadRTPCList(Ar);

            StateGroups = Ar.ReadStateChunk();
            RTPCs = Ar.ReadRTPCList();

            // SwitchContainerGroup
            eGroupType = Ar.Read<byte>();
            ulGroupID = Ar.Read<uint>();
            ulDefaultSwitch = Ar.Read<uint>();
            bIsContinuousValidation = Ar.Read<byte>();

            // Children
            ulNumChilds = Ar.Read<uint>();
            for (int i = 0; i < ulNumChilds; i++)
            {
                ChildIDs.Add(Ar.Read<uint>());
            }
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("ID");
            writer.WriteValue(ulID);

            writer.WritePropertyName("SwitchGroupID");
            writer.WriteValue(ulGroupID);

            writer.WritePropertyName("DefaultSwitchID");
            writer.WriteValue(ulDefaultSwitch);

            writer.WritePropertyName("Children");
            serializer.Serialize(writer, ChildIDs);

            writer.WriteEndObject();
        }
    }

    public class AkPropBundle2
    {
        public readonly byte pID;
        public readonly float pValue;

        public AkPropBundle2(FArchive Ar)
        {
            pID = Ar.Read<byte>();
            pValue = Ar.Read<float>();
        }
    }
}
