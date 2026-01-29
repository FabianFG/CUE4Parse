using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyBus : AbstractHierarchy
{
    public readonly uint OverrideBusId;
    public readonly uint DeviceSharesetId;
    public readonly AkProp[] Props = [];
    public readonly AkPositioningParams? PositioningParams;
    public readonly AkAuxParams? AuxParams;
    public readonly EAdvSettings? AdvSettingsParams;
    public readonly ushort? MaxNumInstance;
    public readonly uint? ChannelConfig;
    public readonly byte? HdrEnvelopeFlags;
    public readonly uint RecoveryTime;
    public readonly float MaxDuckVolume;
    public readonly AkDuckInfo[] DuckInfo = [];
    public readonly AkFxBus FxBusParams;
    public readonly byte OverrideAttachmentParams;
    public readonly AkFxChunk[] FxChunks = [];
    public readonly AkRtpc[] RTPCs;
    public readonly AkStateGroup[] StateGroups;

    public BaseHierarchyBus(FArchive Ar) : base(Ar)
    {
        OverrideBusId = Ar.Read<uint>();
        if (WwiseVersions.Version > 126 && OverrideBusId == 0)
        {
            DeviceSharesetId = Ar.Read<uint>();
        }

        if (WwiseVersions.Version > 56)
        {
            int propCount = Ar.Read<byte>();
            var propIds = Ar.ReadArray(propCount, Ar.Read<byte>);
            var propValues = Ar.ReadArray(propCount, Ar.Read<float>);

            Props = new AkProp[propCount];
            for (int i = 0; i < propCount; i++)
            {
                Props[i] = new AkProp(propIds[i], propValues[i]);
            }
        }

        if (WwiseVersions.Version > 122)
        {
            PositioningParams = new AkPositioningParams(Ar);
            AuxParams = new AkAuxParams(Ar);
        }

        if (WwiseVersions.Version <= 53)
        {
            // TODO: Handle this case
        }
        else if (WwiseVersions.Version <= 122)
        {
            Ar.Read<byte>();
            AdvSettingsParams = Ar.Read<EAdvSettings>();
            MaxNumInstance = Ar.Read<ushort>();
            ChannelConfig = Ar.Read<uint>();
            HdrEnvelopeFlags = Ar.Read<byte>();
        }
        else
        {
            AdvSettingsParams = Ar.Read<EAdvSettings>();
            MaxNumInstance = Ar.Read<ushort>();
            ChannelConfig = Ar.Read<uint>();
            HdrEnvelopeFlags = Ar.Read<byte>();
        }

        if (WwiseVersions.Version <= 56)
        {
            var stateGroupId = Ar.Read<uint>();
        }

        RecoveryTime = Ar.Read<uint>();

        if (WwiseVersions.Version > 38)
        {
            MaxDuckVolume = Ar.Read<float>();
        }

        if (WwiseVersions.Version <= 56)
        {
            var stateSyncType = Ar.Read<uint>();
        }

        DuckInfo = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkDuckInfo(Ar));

        FxBusParams = new AkFxBus(Ar);

        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>();
        }

        if (WwiseVersions.Version > 136)
        {
            FxChunks = Ar.ReadArray(Ar.Read<byte>(), () => new AkFxChunk(Ar));
        }

        RTPCs = AkRtpc.ReadArray(Ar);

        if (WwiseVersions.Version <= 52)
        {
            // State chunk inlined
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else if (WwiseVersions.Version <= 122)
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else
        {
            StateGroups = new AkStateAwareChunk(Ar).Groups;
        }

        if (WwiseVersions.Version <= 126)
        {
            // TODO: FeedbackInfo
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("OverrideBusId");
        writer.WriteValue(OverrideBusId);
        writer.WritePropertyName("DeviceSharesetId");
        writer.WriteValue(DeviceSharesetId);

        writer.WritePropertyName("Props");
        writer.WriteStartArray();
        foreach (var p in Props)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Id");
            writer.WriteValue(p.Id);
            writer.WritePropertyName("Value");
            writer.WriteValue(p.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (PositioningParams != null)
        {
            writer.WritePropertyName("PositioningParams");
            serializer.Serialize(writer, PositioningParams);
        }

        if (AuxParams != null)
        {
            writer.WritePropertyName("AuxParams");
            serializer.Serialize(writer, AuxParams);
        }

        if (AdvSettingsParams.HasValue)
        {
            writer.WritePropertyName("AdvSettingsParams");
            writer.WriteValue(AdvSettingsParams.Value.ToString());
        }

        if (MaxNumInstance.HasValue)
        {
            writer.WritePropertyName("MaxNumInstance");
            writer.WriteValue(MaxNumInstance.Value);
        }

        if (ChannelConfig.HasValue)
        {
            writer.WritePropertyName("ChannelConfig");
            writer.WriteValue(ChannelConfig.Value);
        }

        if (HdrEnvelopeFlags.HasValue)
        {
            writer.WritePropertyName("HdrEnvelopeFlags");
            writer.WriteValue(HdrEnvelopeFlags.Value);
        }

        writer.WritePropertyName("RecoveryTime");
        writer.WriteValue(RecoveryTime);

        writer.WritePropertyName("DuckInfo");
        writer.WriteStartArray();
        foreach (var d in DuckInfo)
            serializer.Serialize(writer, d);
        writer.WriteEndArray();

        writer.WritePropertyName("FxBusParams");
        serializer.Serialize(writer, FxBusParams);

        writer.WritePropertyName("OverrideAttachmentParams");
        writer.WriteValue(OverrideAttachmentParams);

        writer.WritePropertyName("FxChunk");
        writer.WriteStartArray();
        foreach (var f in FxChunks)
            serializer.Serialize(writer, f);
        writer.WriteEndArray();

        writer.WritePropertyName("RtpcList");
        writer.WriteStartArray();
        foreach (var r in RTPCs)
            serializer.Serialize(writer, r);
        writer.WriteEndArray();

        if (StateGroups != null)
        {
            writer.WritePropertyName("StateGroups");
            serializer.Serialize(writer, StateGroups);
        }
    }
}
