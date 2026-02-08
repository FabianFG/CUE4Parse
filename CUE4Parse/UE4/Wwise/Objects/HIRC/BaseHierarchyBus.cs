using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

// CAkBus
public class BaseHierarchyBus : AbstractHierarchy
{
    public readonly uint OverrideBusId;
    public readonly uint DeviceSharesetId;
    public readonly AkProp[] Props = [];
    public readonly AkPositioningParams? PositioningParams;
    public readonly AkAuxParams? AuxParams;
    public readonly EAdvSettings? AdvSettingsParams;
    public readonly ushort? MaxNumInstance;
    public readonly AkChannelConfig ChannelConfig;
    public readonly byte? HdrEnvelopeFlags;
    public readonly uint RecoveryTime;
    public readonly float MaxDuckVolume;
    public readonly AkDuckInfo[] DuckInfo = [];
    public readonly AkFxBus FxBusParams;
    public readonly byte OverrideAttachmentParams;
    public readonly AkFxChunk[] FxChunks = [];
    public readonly AkRtpc[] RTPCs;
    public readonly AkStateGroup[] StateGroups;

    // CAkBus::SetInitialValues
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

        switch (WwiseVersions.Version)
        {
            case <= 53:
                // TODO: Handle this case
                break;
            case <= 122:
                Ar.Read<byte>();
                goto default;
            default:
                AdvSettingsParams = Ar.Read<EAdvSettings>();
                MaxNumInstance = Ar.Read<ushort>();
                ChannelConfig = new AkChannelConfig(Ar);
                HdrEnvelopeFlags = Ar.Read<byte>();
                break;
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
            // FeedbackID = Ar.bFeedbackInBank ? Ar.Read<uint>() : 0;
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName(nameof(OverrideBusId));
        writer.WriteValue(OverrideBusId);
        writer.WritePropertyName(nameof(DeviceSharesetId));
        writer.WriteValue(DeviceSharesetId);

        writer.WritePropertyName(nameof(Props));
        writer.WriteStartArray();
        foreach (var p in Props)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(p.Id));
            writer.WriteValue(p.Id);
            writer.WritePropertyName(nameof(p.Value));
            writer.WriteValue(p.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (PositioningParams != null)
        {
            writer.WritePropertyName(nameof(PositioningParams));
            serializer.Serialize(writer, PositioningParams);
        }

        if (AuxParams != null)
        {
            writer.WritePropertyName(nameof(AuxParams));
            serializer.Serialize(writer, AuxParams);
        }

        if (AdvSettingsParams.HasValue)
        {
            writer.WritePropertyName(nameof(AdvSettingsParams));
            writer.WriteValue(AdvSettingsParams.Value.ToString());
        }

        if (MaxNumInstance.HasValue)
        {
            writer.WritePropertyName(nameof(MaxNumInstance));
            writer.WriteValue(MaxNumInstance.Value);
        }

        writer.WritePropertyName(nameof(ChannelConfig));
        serializer.Serialize(writer, ChannelConfig);

        if (HdrEnvelopeFlags.HasValue)
        {
            writer.WritePropertyName(nameof(HdrEnvelopeFlags));
            writer.WriteValue(HdrEnvelopeFlags.Value);
        }

        writer.WritePropertyName(nameof(RecoveryTime));
        writer.WriteValue(RecoveryTime);

        writer.WritePropertyName(nameof(DuckInfo));
        writer.WriteStartArray();
        foreach (var d in DuckInfo)
            serializer.Serialize(writer, d);
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(FxBusParams));
        serializer.Serialize(writer, FxBusParams);

        writer.WritePropertyName(nameof(OverrideAttachmentParams));
        writer.WriteValue(OverrideAttachmentParams);

        writer.WritePropertyName(nameof(FxChunks));
        writer.WriteStartArray();
        foreach (var f in FxChunks)
            serializer.Serialize(writer, f);
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(RTPCs));
        writer.WriteStartArray();
        foreach (var r in RTPCs)
            serializer.Serialize(writer, r);
        writer.WriteEndArray();

        if (StateGroups != null)
        {
            writer.WritePropertyName(nameof(StateGroups));
            serializer.Serialize(writer, StateGroups);
        }
    }
}
