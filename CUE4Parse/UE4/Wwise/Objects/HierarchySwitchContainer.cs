using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Readers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Wwise.Objects
{

    public class HierarchySwitchContainer : AbstractHierarchy
    {
        public AkFXParams FXChain { get; private set; }

        public readonly byte OverrideParentMetadataFlag;
        public readonly byte NumFXMetadataFlag;

        public readonly uint OverrideBusId;
        public readonly uint DirectParentID;

        public readonly ESwitchContainer BitVectorSwitch;

        public List<AkProp> Props { get; private set; }
        public List<AkPropRange> PropRanges { get; private set; }

        public AkPositioningParams PositioningParams { get; private set; }

        public readonly EBitsPositioning BitsPositioning;

        public readonly EAuxParams AuxParams;
        public readonly uint ReflectionsAuxBus;

        public readonly EAdvSettings AdvSettingsParams;
        public readonly byte VirtualQueueBehavior;
        public readonly ushort MaxNumInstance;
        public readonly byte BelowThresholdBehavior;
        public readonly byte HdrEnvelopeBitVector;

        public List<AkStateGroup> StateGroups { get; private set; }
        public List<AkRTPC> RTPCs { get; private set; }

        public readonly byte GroupType;
        public readonly uint GroupId;
        public readonly uint DefaultSwitch;
        public readonly byte IsContinuousValidation;

        public readonly List<uint> ChildIDs = [];

        public HierarchySwitchContainer(FArchive Ar) : base(Ar)
        {
            FXChain = new AkFXParams(Ar);

            OverrideParentMetadataFlag = Ar.Read<byte>();
            NumFXMetadataFlag = Ar.Read<byte>();
            if (WwiseVersions.WwiseVersion <= 145)
                Ar.Read<byte>();

            OverrideBusId = Ar.Read<uint>();
            DirectParentID = Ar.Read<uint>();

            BitVectorSwitch = Ar.Read<ESwitchContainer>();

            AkPropBundle propBundle = new(Ar);
            Props = propBundle.Props;
            PropRanges = propBundle.PropRanges;

            PositioningParams = new AkPositioningParams(Ar);

            AuxParams = Ar.Read<EAuxParams>();
            if (AuxParams.HasFlag(EAuxParams.HasAux))
                for (int i = 0; i < 4; i++)
                    Ar.Read<uint>();
            ReflectionsAuxBus = Ar.Read<uint>();

            AdvSettingsParams = Ar.Read<EAdvSettings>();
            VirtualQueueBehavior = Ar.Read<byte>();
            MaxNumInstance = Ar.Read<ushort>();
            BelowThresholdBehavior = Ar.Read<byte>();
            HdrEnvelopeBitVector = Ar.Read<byte>();

            StateGroups = new AkStateChunk(Ar).Groups;
            RTPCs = new AkRTPCList(Ar);

            GroupType = Ar.Read<byte>();
            GroupId = Ar.Read<uint>();
            DefaultSwitch = Ar.Read<uint>();
            IsContinuousValidation = Ar.Read<byte>();

            var numChilds = Ar.Read<uint>();
            for (int i = 0; i < numChilds; i++)
            {
                ChildIDs.Add(Ar.Read<uint>());
            }
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

            writer.WritePropertyName("BitVectorSwitch");
            writer.WriteValue(BitVectorSwitch.ToString());

            writer.WritePropertyName("Props");
            serializer.Serialize(writer, Props);

            writer.WritePropertyName("PropRanges");
            serializer.Serialize(writer, PropRanges);

            writer.WritePropertyName("PositioningParams");
            serializer.Serialize(writer, PositioningParams);

            writer.WritePropertyName("AuxParams");
            writer.WriteValue(AuxParams.ToString());

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

            writer.WritePropertyName("HdrEnvelopeBitVector");
            writer.WriteValue(HdrEnvelopeBitVector);

            writer.WritePropertyName("StateGroups");
            serializer.Serialize(writer, StateGroups);

            writer.WritePropertyName("RTPCs");
            serializer.Serialize(writer, RTPCs);

            writer.WritePropertyName("GroupType");
            writer.WriteValue(GroupType);

            writer.WritePropertyName("GroupId");
            writer.WriteValue(GroupId);

            writer.WritePropertyName("DefaultSwitch");
            writer.WriteValue(DefaultSwitch);

            writer.WritePropertyName("IsContinuousValidation");
            writer.WriteValue(IsContinuousValidation != 0);

            writer.WritePropertyName("Children");
            serializer.Serialize(writer, ChildIDs);

            writer.WriteEndObject();
        }
    }
}
