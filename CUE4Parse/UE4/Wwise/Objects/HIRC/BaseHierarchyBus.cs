using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyBus : AbstractHierarchy
{
    public uint OverrideBusId { get; protected set; }
    public uint DeviceSharesetId { get; protected set; }
    public List<AkProp> Props { get; protected set; } = [];
    //public List<AkPropRange>? PropRanges { get; protected set; }
    public AkPositioningParams? PositioningParams { get; protected set; }
    public AkAuxParams? AuxParams { get; protected set; }

    public EAdvSettings? AdvSettingsParams { get; protected set; }
    public ushort? MaxNumInstance { get; protected set; }
    public uint? ChannelConfig { get; protected set; }
    public byte? HdrEnvelopeFlags { get; protected set; }

    public uint RecoveryTime { get; protected set; }

    public List<AkDuckInfo> DuckInfo { get; protected set; } = [];

    public AkFXParams FXParams { get; protected set; }

    public byte OverrideAttachmentParams { get; protected set; }

    public List<AkFXChunk> FXChunk { get; protected set; } = [];

    public List<AkRTPC> RTPCs { get; protected set; }

    public List<AkStateGroup>? StateGroups { get; protected set; }

    public BaseHierarchyBus(FArchive Ar) : base(Ar)
    {
        OverrideBusId = Ar.Read<uint>();
        if (WwiseVersions.WwiseVersion > 126 && OverrideBusId == 0)
        {
            DeviceSharesetId = Ar.Read<uint>();
        }

        if (WwiseVersions.WwiseVersion > 56)
        {
            int propCount = Ar.Read<byte>();
            var propIds = new List<byte>(propCount);
            var propValues = new List<float>(propCount);

            for (int i = 0; i < propCount; i++)
            {
                var propId = Ar.Read<byte>();
                propIds.Add(propId);
            }

            for (int i = 0; i < propCount; i++)
            {
                var propValue = Ar.Read<float>();
                propValues.Add(propValue);
            }

            Props = new List<AkProp>(propCount);
            for (int i = 0; i < propCount; i++)
            {
                Props.Add(new AkProp(propIds[i], propValues[i]));
            }
        }

        if (WwiseVersions.WwiseVersion > 122)
        {
            PositioningParams = new AkPositioningParams(Ar);
            AuxParams = new AkAuxParams(Ar);
        }

        if (WwiseVersions.WwiseVersion <= 53)
        {
            // TODO: Handle this case
        }
        else
        {
            AdvSettingsParams = Ar.Read<EAdvSettings>();
            MaxNumInstance = Ar.Read<ushort>();
            ChannelConfig = Ar.Read<uint>();
            HdrEnvelopeFlags = Ar.Read<byte>();
        }

        if (WwiseVersions.WwiseVersion <= 56)
        {
            var stateGroupId = Ar.Read<uint>();
        }

        RecoveryTime = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 38)
        {
            var maxDuckVolume = Ar.Read<float>();
        }

        if (WwiseVersions.WwiseVersion <= 56)
        {
            var stateSyncType = Ar.Read<uint>();
        }

        var numDucks = Ar.Read<uint>();
        for (int i = 0; i < numDucks; i++)
        {
            DuckInfo.Add(new AkDuckInfo(Ar));
        }

        //OverrideFX = Ar.Read<byte>() != 0;
        FXParams = new AkFXParams(Ar);

        if (WwiseVersions.WwiseVersion > 89 && WwiseVersions.WwiseVersion <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>();
        }

        if (WwiseVersions.WwiseVersion > 136)
        {
            var numFx = Ar.Read<byte>();
            if (numFx > 0)
            {
                for (int i = 0; i < numFx; i++)
                {
                    FXChunk.Add(new AkFXChunk(Ar));
                }
            }
        }

        RTPCs = AkRTPC.ReadMultiple(Ar);

        if (WwiseVersions.WwiseVersion <= 56)
        {
            // TODO: State chunk inlined
        }
        else
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }

        if (WwiseVersions.WwiseVersion <= 126)
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
            writer.WriteValue(AdvSettingsParams.Value);
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

        writer.WritePropertyName("FXParams");
        serializer.Serialize(writer, FXParams);

        writer.WritePropertyName("OverrideAttachmentParams");
        writer.WriteValue(OverrideAttachmentParams);

        writer.WritePropertyName("FXChunk");
        writer.WriteStartArray();
        foreach (var f in FXChunk)
            serializer.Serialize(writer, f);
        writer.WriteEndArray();

        writer.WritePropertyName("RTPCs");
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
