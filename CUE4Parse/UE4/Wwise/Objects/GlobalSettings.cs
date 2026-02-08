using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class GlobalSettings
{
    public readonly EAkFilterBehavior FilterBehavior;
    public readonly float VolumeThreshold;
    public readonly ushort MaxNumVoicesLimitInternal;
    public readonly ushort MaxNumDangerousVirtVoicesLimitInternal;
    public readonly float HSFEmphasis;
    public readonly AkStateGroupInfo[] StateGroups;
    public readonly AkSwitchGroup[] SwitchGroups;
    public readonly AkRTPCRamping[] RTPCRampingParams = [];
    public List<ICAkIndexable> VirtualAcoustics = [];

    // CAkBankMgr::ProcessGlobalSettingsChunk
    public GlobalSettings(FArchive Ar)
    {
        // AkFilterBehavior::SetInternal
        if (WwiseVersions.Version > 140)
            FilterBehavior = Ar.Read<EAkFilterBehavior>();

        // AK::SoundEngine::SetVolumeThresholdInternal
        VolumeThreshold = Ar.Read<float>();

        // AK::SoundEngine::SetMaxNumVoicesLimitInternal
        if (WwiseVersions.Version > 53)
            MaxNumVoicesLimitInternal = Ar.Read<ushort>();

        // AK::SoundEngine::SetMaxNumDangerousVirtVoicesLimitInternal
        if (WwiseVersions.Version > 126)
            MaxNumDangerousVirtVoicesLimitInternal = Ar.Read<ushort>();

        // AK::SoundEngine::SetHSFEmphasis
        if (WwiseVersions.Version > 154)
            HSFEmphasis = Ar.Read<float>();

        // CAkStateMgr::AddStateGroup
        StateGroups = Ar.ReadArray(() => new AkStateGroupInfo(Ar));
        SwitchGroups = Ar.ReadArray(() => new AkSwitchGroup(Ar));
        if (WwiseVersions.Version <= 38)
            return;
        RTPCRampingParams = Ar.ReadArray(() => new AkRTPCRamping(Ar));

        // CAkVirtualAcousticsMgr::AddAcousticTexture
        VirtualAcoustics.AddRange(WwiseVersions.Version switch
        {
            <= 118 => [],
            <= 122 => Ar.ReadArray<ICAkIndexable>(() => new AkAcousticTexture_v122(Ar)),
            _ => Ar.ReadArray<ICAkIndexable>(() => new AkAcousticTexture(Ar))
        });

        if (WwiseVersions.Version is > 118 and <= 122)
        {
            VirtualAcoustics.AddRange(Ar.ReadArray<ICAkIndexable>(() => new AkDiffuseReverberator(Ar)));
        }
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(FilterBehavior));
        writer.WriteValue(FilterBehavior.ToString());

        writer.WritePropertyName(nameof(VolumeThreshold));
        writer.WriteValue(VolumeThreshold);

        writer.WritePropertyName(nameof(MaxNumVoicesLimitInternal));
        writer.WriteValue(MaxNumVoicesLimitInternal);

        writer.WritePropertyName(nameof(MaxNumDangerousVirtVoicesLimitInternal));
        writer.WriteValue(MaxNumDangerousVirtVoicesLimitInternal);

        writer.WritePropertyName(nameof(HSFEmphasis));
        writer.WriteValue(HSFEmphasis);

        writer.WritePropertyName(nameof(StateGroups));
        serializer.Serialize(writer, StateGroups);

        writer.WritePropertyName(nameof(SwitchGroups));
        serializer.Serialize(writer, SwitchGroups);

        writer.WritePropertyName(nameof(RTPCRampingParams));
        serializer.Serialize(writer, RTPCRampingParams);

        writer.WritePropertyName(nameof(VirtualAcoustics));
        serializer.Serialize(writer, VirtualAcoustics);

        writer.WriteEndObject();
    }
}
