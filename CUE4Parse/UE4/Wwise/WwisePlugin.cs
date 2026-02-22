using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Plugins;
using CUE4Parse.UE4.Wwise.Plugins.atmoky;
using CUE4Parse.UE4.Wwise.Plugins.Auro;
using CUE4Parse.UE4.Wwise.Plugins.CrankcaseAudioREVModelPlayer;
using CUE4Parse.UE4.Wwise.Plugins.iZotope;
using CUE4Parse.UE4.Wwise.Plugins.MasteringSuite;
using CUE4Parse.UE4.Wwise.Plugins.McDSP;
using CUE4Parse.UE4.Wwise.Plugins.MetaXRAudio;
using CUE4Parse.UE4.Wwise.Plugins.Mindseye;
using CUE4Parse.UE4.Wwise.Plugins.OculusSpatializer;
using CUE4Parse.UE4.Wwise.Plugins.PolyspectralMBC;
using CUE4Parse.UE4.Wwise.Plugins.ResonanceAudio;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwisePlugin
{
    public static IAkPluginParam? TryParsePluginParams(FArchive Ar, AkPlugin? plugin, bool always = false)
    {
        if (plugin is null)
            return null;
        var pluginId = plugin.Value.PluginId;
        if (pluginId is EAkPluginId.None)
            return null;
        if ((int) pluginId < 0 && !always)
            return null;

        uint size = Ar.Read<uint>();
        if (size == 0)
            return null;

        var saved = Ar.Position;
        var endPosition = saved + size;
        IAkPluginParam? Params = null;
        try
        {
            Params = pluginId switch
            {
                // Built-in Wwise plugins
                EAkPluginId.AkFxSrcSineSource => new CAkFxSrcSineParams(Ar),
                EAkPluginId.AkFxSrcSilenceSource => new CAkFxSrcSilenceParams(Ar),
                EAkPluginId.AkToneSource => new CAkToneGenParams(Ar),
                EAkPluginId.AkParameterEQFX => new CAkParameterEQFXParams(Ar),
                EAkPluginId.AkDelayFX => new CAkDelayFXParams(Ar),
                EAkPluginId.AkCompressorFX => new CAkCompressorFXParams(Ar),
                EAkPluginId.AkExpanderFX => new CAkExpanderFXParams(Ar),
                EAkPluginId.AkPeakLimiterFX => new CAkPeakLimiterFXParams(Ar),

                EAkPluginId.AkMatrixReverbFX => new CAkFDNReverbFXParams(Ar),
                EAkPluginId.AkSoundSeedImpactFX => new CAkModalSynthParams(Ar),
                EAkPluginId.AkRoomVerbFX => new CAkRoomVerbFXParams(Ar),
                EAkPluginId.AkSoundSeedWind => new CAkSoundSeedWindParams(Ar),
                EAkPluginId.AkSoundSeedWoosh => new CAkSoundSeedWooshParams(Ar),
                EAkPluginId.AkFlangerFX => new CAkFlangerFXParams(Ar),
                EAkPluginId.AkGuitarDistortionFX => new CAkGuitarDistortionFXParams(Ar),
                EAkPluginId.AkConvolutionReverbFX => new CAkConvolutionReverbFXParams(Ar),

                EAkPluginId.AkMeterFX => new CAkMeterFXParams(Ar),
                EAkPluginId.AkTimeStretchFX => new CAkTimeStretchFXParams(Ar),
                EAkPluginId.AkTremoloFX => new CAkTremoloFXParams(Ar),
                EAkPluginId.AkRecorderFX => new CAkRecorderFXParams(Ar, (int) size),
                EAkPluginId.AkStereoDelayFX => new CAkStereoDelayFXParams(Ar),
                EAkPluginId.AkPitchShifterFX => new CAkPitchShifterFXParams(Ar),
                EAkPluginId.AkHarmonizerFX => new CAkHarmonizerFXParams(Ar),
                EAkPluginId.AkGainFX => new CAkGainFXParams(Ar),

                EAkPluginId.AkSynthOne => new CAkSynthOneParams(Ar),

                EAkPluginId.ASIOSink => new CAkAsioSinkParams(Ar),
                EAkPluginId.MicrosoftHRTFSink => new CMicrosoftHRTFSinkParams(Ar),
                EAkPluginId.AkReflectFX => new CAkReflectFXParams(Ar),
                // EAkPluginId.AkRouterMixer

                EAkPluginId.SystemSink => new CAkSystemSinkParams(Ar),
                EAkPluginId.DVRByPassSink => new CAkDVRSinkParams(Ar),
                EAkPluginId.CommunicationSink or EAkPluginId.ControllerHeadphonesSink or  EAkPluginId.VoiceSink or
                    EAkPluginId.ControllerSpeakerSink or EAkPluginId.AuxiliarySink or EAkPluginId.NoOutputSink or
                    EAkPluginId.RemoteSystemSink => new CAkDefaultSinkParams(),

                // EAkPluginId.AkSoundSeedGrainSrc
                EAkPluginId.AkImpacterSource => new CAkImpacterParams(Ar),
                EAkPluginId.MasteringSuiteFX => new CMasteringSuiteFXParams(Ar),
                EAkPluginId.Ak3DAudioBedMixerFX => new CAk3DAudioBedMixerFXParams(Ar),
                EAkPluginId.AkChannelRouterFX => new CAkChannelRouterFXParams(Ar),

                EAkPluginId.AkSidechainSendFX => new CAkSidechainSendFXParams(Ar),
                EAkPluginId.AkSidechainRecvFX => new CAkSidechainRecvFXParams(Ar),
                EAkPluginId.AkMultibandMeterFX => new CAkMultibandMeterFXParams(Ar),
                EAkPluginId.AkRecorder_ADM => new CAkRecorderADMFXParams(Ar),
                EAkPluginId.AkAudioInputSource => new CAkFxSrcAudioInputParams(Ar),
                EAkPluginId.ASIOSource => new CAkAsioSourceParams(Ar),

                EAkPluginId.AkMotionGeneratorSource or EAkPluginId.AkMotionGeneratorMotionSource => new CAkMotionGeneratorParams(Ar),
                EAkPluginId.AkMotionSourceSource or EAkPluginId.AkMotionSource => new CAkMotionSourceParams(Ar),
                EAkPluginId.AkMotionSink => new CAkDefaultSinkParams(),

                EAkPluginId.AkSystemOutputMeta => new CAkSystemOutputParams(Ar),

                EAkPluginId.atmokyEars => new CAtmokyEarsFXParams(Ar),

                // EAkPluginId.AudioSpectrumFX
                EAkPluginId.AuroHeadphoneFX => new CAuroHPFXParams(Ar),
                EAkPluginId.AuroPannerFX => new CAuroPannerFXParams(Ar),
                EAkPluginId.AuroPannerMixer => new CAuroPannerMixerParams(Ar),

                // EAkPluginId.bnsRadio

                EAkPluginId.CrankcaseAudioREVModelPlayer => new CREVSourceModelPlayerParams(Ar, (int)size),                

                EAkPluginId.iZHybridReverbFX => new CiZHybridReverbFXParams(Ar),
                EAkPluginId.iZTrashDistortionFX => new CiZTrashDistortionFXParams(Ar),
                EAkPluginId.iZTrashDelayFX => new CiZTrashDelayFXParams(Ar),
                EAkPluginId.iZTrashDynamicsFX => new CiZTrashDynamicsFXParams(Ar),
                EAkPluginId.iZTrashFiltersFX => new CiZTrashFiltersFXParams(Ar),
                EAkPluginId.iZTrashBoxModelerFX => new CiZTrashBoxModelerFXParams(Ar),
                EAkPluginId.iZTrashMultibandDistortionFX => new CiZTrashMultibandDistortionFXParams(Ar),

                EAkPluginId.AudioDataPassbackFX => new AudioDataPassbackFXParams(Ar),
                EAkPluginId.BarbDelayFX => new BarbDelayFXParams(Ar),
                EAkPluginId.BarbRecorderFX => new BarbRecorderFXParams(Ar),
                EAkPluginId.DrunkPMSource => new DrunkPMSourceParams(Ar),

                EAkPluginId.MsSpatialSink => new CAkDefaultSinkParams(),

                EAkPluginId.McDSPLimiterFX => new CMcDSPLimiterFXParams(Ar),
                EAkPluginId.McDSPFutzBoxFX => new CMcDSPFutzBoxFXParams(Ar),

                EAkPluginId.OculusAttachableMixerInputFX => new COculusSpatializerFXAttachmentParams(Ar),
                EAkPluginId.OculusEndpointSink => new OculusEndpointSinkParams(Ar),
                EAkPluginId.OculusEndpointMetadata => new OculusEndpointMetadataParams(Ar),
                EAkPluginId.OculusEndpointExperimentalMetadata => new OculusEndpointExperimentalMetadataParams(Ar),
                EAkPluginId.OculusSpatializerMixer => new COculusSpatializerFXParams(Ar),

                EAkPluginId.PolyspectralMBC => new CMBCRuntimeParams(Ar, (int)size),

                EAkPluginId.ResonanceAudioRendererFX or EAkPluginId.ResonanceAudioRoomEffectMixer or
                    EAkPluginId.ResonanceAudioRoomEffectFX => new ResonanceAudioParams(Ar),

                // EAkPluginId.IgniterLive
                // EAkPluginId.IgniterLiveSynth

                EAkPluginId.TencentGMESendFX or EAkPluginId.TencentGMESource or
                    EAkPluginId.TencentGMEReceiveSource => new CAkDefaultSinkParams(),
                // EAkPluginId.TencentGMESessionFX

                _ => new CAkDefaultParams(Ar, (int)size),
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error while parsing Wwise plugin '{pluginId}' with WWise version {WwiseVersions.Version}");
        }
        finally
        {
#if DEBUG
            if (Params is CAkDefaultParams)
            {
                Log.Warning($"Handler for Wwise plugin '{pluginId}' wasn't added, skipping {size} bytes");
            }

            if (Ar.Position != endPosition)
            {
                Log.Warning($"Didn't read Wwise plugin '{pluginId}' with WWise version {WwiseVersions.Version} correctly (at {Ar.Position}, should be {endPosition})");
            }
#endif
            Ar.Position = endPosition;
        }

        return Params;
    }

    public static AkPlugin GetPluginId(FArchive Ar)
    {
        uint rawId = Ar.Read<uint>();
        if (rawId is uint.MaxValue || rawId is 0)
            return AkPlugin.None;

        return new AkPlugin(rawId);
    }
}

public readonly struct AkPlugin(uint rawId)
{
    private readonly uint _raw = rawId;

    public static readonly AkPlugin None = new(uint.MaxValue);

    public EAkPluginId PluginId => (EAkPluginId) _raw;
    public AkCompanyID CompanyId => IsValid ? (AkCompanyID) ((_raw >> 4) & 0xFF) : AkCompanyID.Audiokinetic;
    public EAkPluginType Type => IsValid ? (EAkPluginType) (_raw & 0xF) : EAkPluginType.None;

    [JsonIgnore]
    public bool IsValid => _raw is not uint.MaxValue && _raw is not 0;
}
