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
    public readonly EAdvSettingsFlags? AdvSettingsParams;
    public readonly ushort? MaxNumInstance;
    public readonly AkChannelConfig ChannelConfig;
    public readonly byte? HdrEnvelopeFlags;
    public readonly int RecoveryTime;
    public readonly float MaxDuckVolume;
    public readonly AkDuckInfo[] DuckInfo = [];
    public readonly AkFxBus FxBusParams;
    public readonly byte OverrideAttachmentParams;
    public readonly AkFxChunk[] FxChunks = [];
    public readonly AkFxChunk[] MetadataParams = [];
    public readonly AkRtpc[] RTPCs;
    public readonly AkStateGroup[] StateGroups;
    public readonly AkFeedbackInfo? FeedbackInfo;

    // CAkBus::SetInitialValues
    public BaseHierarchyBus(FWwiseArchive Ar) : base()
    {
        Id = Ar.Read<uint>();
        OverrideBusId = Ar.Read<uint>();
        if (Ar.Version > 126 && OverrideBusId == 0)
        {
            DeviceSharesetId = Ar.Read<uint>();
        }

        if (Ar.Version > 56)
        {
            Props = AkPropBundle.ReadSequentialAkProp(Ar);
        }

        if (Ar.Version > 122)
        {
            PositioningParams = new AkPositioningParams(Ar);
            AuxParams = new AkAuxParams(Ar);
        }

        switch (Ar.Version)
        {
            case <= 53:
            {
                Ar.Read<float>(); // VolumeMain
                Ar.Read<float>(); // LFEVolumeMain
                Ar.Read<float>(); // PitchMain
                Ar.Read<float>(); // LPFMain

                var killNewest = Ar.ReadBool();
                MaxNumInstance = Ar.Read<ushort>();
                var isMaxNumInstOverrideParent = Ar.ReadBool();

                if (Ar.Version > 48)
                    ChannelConfig = new AkChannelConfig(Ar.Read<ushort>());

                Ar.Read<byte>();
                Ar.Read<byte>();

                if (Ar.Version > 48)
                    _ = Ar.ReadBool(); // IsEnvironmental

                AdvSettingsParams = EAdvSettingsFlags.None;
                if (killNewest)
                    AdvSettingsParams |= EAdvSettingsFlags.KillNewest;
                if (isMaxNumInstOverrideParent)
                    AdvSettingsParams |= EAdvSettingsFlags.IgnoreParentMaxNumInst;
                break;
            }
            case <= 56:
            {
                Ar.Read<float>(); // VolumeMain
                Ar.Read<float>(); // LFEVolumeMain
                Ar.Read<float>(); // PitchMain
                Ar.Read<float>(); // LPFMain

                var killNewest = Ar.ReadBool();
                var useVirtualBehavior = Ar.ReadBool();
                MaxNumInstance = Ar.Read<ushort>();
                var isMaxNumInstOverrideParent = Ar.ReadBool();

                ChannelConfig = new AkChannelConfig(Ar.Read<ushort>());

                Ar.Read<byte>();
                Ar.Read<byte>();

                _ = Ar.ReadBool(); // bIsEnvBus

                AdvSettingsParams = EAdvSettingsFlags.None;
                if (killNewest)
                    AdvSettingsParams |= EAdvSettingsFlags.KillNewest;
                if (useVirtualBehavior)
                    AdvSettingsParams |= EAdvSettingsFlags.UseVirtualBehavior;
                if (isMaxNumInstOverrideParent)
                    AdvSettingsParams |= EAdvSettingsFlags.IgnoreParentMaxNumInst;
                break;
            }
            case <= 65:
            {
                var killNewest = Ar.ReadBool();
                var useVirtualBehavior = Ar.ReadBool();
                MaxNumInstance = Ar.Read<ushort>();
                var isMaxNumInstOverrideParent = Ar.ReadBool();

                ChannelConfig = new AkChannelConfig(Ar.Read<ushort>());

                Ar.Read<byte>();
                Ar.Read<byte>();

                _ = Ar.ReadBool(); // bIsEnvBus

                AdvSettingsParams = EAdvSettingsFlags.None;
                if (killNewest)
                    AdvSettingsParams |= EAdvSettingsFlags.KillNewest;
                if (useVirtualBehavior)
                    AdvSettingsParams |= EAdvSettingsFlags.UseVirtualBehavior;
                if (isMaxNumInstOverrideParent)
                    AdvSettingsParams |= EAdvSettingsFlags.IgnoreParentMaxNumInst;
                break;
            }
            case <= 77:
            {
                var killNewest = Ar.ReadBool();
                var useVirtualBehavior = Ar.ReadBool();
                MaxNumInstance = Ar.Read<ushort>();
                var isMaxNumInstOverrideParent = Ar.ReadBool();

                ChannelConfig = new AkChannelConfig(Ar.Read<ushort>());

                Ar.Read<byte>();
                Ar.Read<byte>();

                AdvSettingsParams = EAdvSettingsFlags.None;

                if (killNewest)
                    AdvSettingsParams |= EAdvSettingsFlags.KillNewest;
                if (useVirtualBehavior)
                    AdvSettingsParams |= EAdvSettingsFlags.UseVirtualBehavior;
                if (isMaxNumInstOverrideParent)
                    AdvSettingsParams |= EAdvSettingsFlags.IgnoreParentMaxNumInst;
                break;
            }
            case <= 89:
            {
                Ar.Read<byte>(); // PositioningEnabled
                Ar.Read<byte>(); // PositioningEnablePanner

                var killNewest = Ar.ReadBool();
                var useVirtualBehavior = Ar.ReadBool();
                MaxNumInstance = Ar.Read<ushort>();
                var isMaxNumInstOverrideParent = Ar.ReadBool();

                ChannelConfig = new AkChannelConfig(Ar.Read<ushort>());

                Ar.Read<byte>();
                Ar.Read<byte>();

                var isHdrBus = Ar.ReadBool();
                var hdrReleaseModeExponential = Ar.ReadBool();

                AdvSettingsParams = EAdvSettingsFlags.None;
                if (killNewest)
                    AdvSettingsParams |= EAdvSettingsFlags.KillNewest;
                if (useVirtualBehavior)
                    AdvSettingsParams |= EAdvSettingsFlags.UseVirtualBehavior;
                if (isMaxNumInstOverrideParent)
                    AdvSettingsParams |= EAdvSettingsFlags.IgnoreParentMaxNumInst;

                // Set HdrEnvelopeFlags
                //if (isHdrBus)
                //if (hdrReleaseModeExponential)
                break;
            }
            case <= 122:
            {
                Ar.Read<byte>();
                AdvSettingsParams = Ar.Read<EAdvSettingsFlags>();
                MaxNumInstance = Ar.Read<ushort>();
                ChannelConfig = new AkChannelConfig(Ar);
                HdrEnvelopeFlags = Ar.Read<byte>();
                break;
            }
            default:
            {
                AdvSettingsParams = Ar.Read<EAdvSettingsFlags>();
                MaxNumInstance = Ar.Read<ushort>();
                ChannelConfig = new AkChannelConfig(Ar);
                HdrEnvelopeFlags = Ar.Read<byte>();
                break;
            }
        }

        if (Ar.Version <= 52)
        {
            Ar.Read<uint>(); // stateGroupId
        }

        RecoveryTime = Ar.Read<int>();

        if (Ar.Version > 38)
        {
            MaxDuckVolume = Ar.Read<float>();
        }

        if (Ar.Version <= 52)
        {
            Ar.Read<uint>(); // stateSyncType
        }

        DuckInfo = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkDuckInfo(Ar));

        FxBusParams = new AkFxBus(Ar);

        if (Ar.Version > 89 && Ar.Version <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>();
        }

        if (Ar.Version > 136)
        {
            MetadataParams = Ar.ReadArray(Ar.Read<byte>(), () => new AkFxChunk(Ar));
        }

        RTPCs = AkRtpc.ReadArray(Ar);

        if (Ar.Version <= 52)
        {
            // State chunk inlined
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else if (Ar.Version <= 122)
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }
        else
        {
            StateGroups = new AkStateAwareChunk(Ar).Groups;
        }

        if (Ar.Version <= 126 && Ar.HasFeedback)
        {
            FeedbackInfo = new AkFeedbackInfo(Ar);
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
            writer.WriteValue(p.Value.Value);
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

        writer.WritePropertyName(nameof(MetadataParams));
        writer.WriteStartArray();
        foreach (var m in MetadataParams)
            serializer.Serialize(writer, m);
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
