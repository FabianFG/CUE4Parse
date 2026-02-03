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
    public readonly AkRTPCRamping[] RTPCRampingParams;
    public readonly AkAcousticTexture_v122[] AcousticTextures_v122 = [];
    public readonly AkAcousticTexture[] AcousticTextures = [];

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
        StateGroups = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkStateGroupInfo(Ar));
        SwitchGroups = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkSwitchGroup(Ar));
        RTPCRampingParams = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkRTPCRamping(Ar));

        // CAkVirtualAcousticsMgr::AddAcousticTexture
        switch (WwiseVersions.Version)
        {
            case <= 118:
                // Nothing
                break;
            case <= 122:
                AcousticTextures_v122 = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkAcousticTexture_v122(Ar));
                break;
            default:
                AcousticTextures = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkAcousticTexture(Ar));
                break;
        }

        switch (WwiseVersions.Version)
        {
            case <= 118:
                // Nothing
                break;
            case <= 122:
                Ar.ReadArray((int) Ar.Read<uint>(), () => new AkDiffuseReverberator(Ar));
                break;
            default:
                // Nothing
                break;
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

        if (WwiseVersions.Version > 122)
        {
            writer.WritePropertyName(nameof(AcousticTextures));
            serializer.Serialize(writer, AcousticTextures);
        }
        else
        {
            writer.WritePropertyName(nameof(AcousticTextures_v122));
            serializer.Serialize(writer, AcousticTextures_v122);
        }

        writer.WriteEndObject();
    }
}
