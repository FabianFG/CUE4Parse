using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Plugins;
using CUE4Parse.UE4.Wwise.Plugins.Auro;
using CUE4Parse.UE4.Wwise.Plugins.iZotope;
using CUE4Parse.UE4.Wwise.Plugins.MasteringSuite;
using CUE4Parse.UE4.Wwise.Plugins.McDSP;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwisePlugin
{
    public static IAkPluginParam? TryParsePluginParams(FArchive Ar, EAkPluginId pluginId, bool always = false)
    {
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
        var temp = Ar.ReadBytes((int) size);
        
        Ar.Position = saved;
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

                EAkPluginId.AkReflectFX => new CAkReflectFXParams(Ar),
                // EAkPluginId.AkRouterMixer
                EAkPluginId.SystemSink => new CAkDefaultSinkParams(),
                // EAkPluginId.DVRByPassSink

                // EAkPluginId.CommunicationSink
                // EAkPluginId.ControllerHeadphonesSink
                // EAkPluginId.ControllerSpeakerSink
                // EAkPluginId.AuxiliarySink
                EAkPluginId.NoOutputSink => new CAkDefaultSinkParams(),
                // EAkPluginId.AkSoundSeedGrainSrc
                // EAkPluginId.AkImpacterSrc
                EAkPluginId.MasteringSuiteFX => new CMasteringSuiteFXParams(Ar),
                EAkPluginId.Ak3DAudioBedMixerFX => new CAk3DAudioBedMixerFXParams(Ar),
                // EAkPluginId.AkChannelRouterFX

                // EAkPluginId.AkMultibandMeterFX
                EAkPluginId.AkAudioInputSrc => new CAkFxSrcAudioInputParams(Ar),

                EAkPluginId.AkMotionGeneratorSource => new CAkMotionGeneratorParams(Ar),
                // EAkPluginId.AkMotionGeneratorMotionSource
                EAkPluginId.AkMotionSourceSource => new CAkMotionSourceParams(Ar),
                // EAkPluginId.AkMotionSource
                EAkPluginId.AkMotionSink => new CAkMotionSinkParams(),

                // EAkPluginId.AkSystemoutputSettings

                EAkPluginId.AuroHeadphoneFX => new CAuroHPFXParams(Ar),

                // EAkPluginId.CrankcaseAudioREVModelPlayer
                // EAkPluginId.AudioSpectrumFX
                // EAkPluginId.bnsRadio

                EAkPluginId.iZHybridReverbFX => new CiZHybridReverbFXParams(Ar),
                EAkPluginId.iZTrashDistortionFX => new CiZTrashDistortionFXParams(Ar),
                EAkPluginId.iZTrashDelayFX => new CiZTrashDelayFXParams(Ar),
                EAkPluginId.iZTrashDynamicsFX => new CiZTrashDynamicsFXParams(Ar),
                EAkPluginId.iZTrashFiltersFX => new CiZTrashFiltersFXParams(Ar),
                EAkPluginId.iZTrashBoxModelerFX => new CiZTrashBoxModelerFXParams(Ar),
                EAkPluginId.iZTrashMultibandDistortionFX => new CiZTrashMultibandDistortionFXParams(Ar),

                EAkPluginId.McDSPLimiterFX => new CMcDSPLimiterFXParams(Ar),
                EAkPluginId.McDSPFutzBoxFX => new CMcDSPFutzBoxFXParams(Ar),

                // EAkPluginId.ResonanceAudioRendererFX
                // EAkPluginId.ResonanceAudioRoomEffect

                // EAkPluginId.IgniterLive
                // EAkPluginId.IgniterLiveSynth

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

    // Actual Plugin ID Format: 0xPPPPCCCT (PPPP = PluginID, CCC=CompanyID, T=Type)
    public static uint GetPluginId(FArchive Ar)
    {
        var pluginId = Ar.Read<uint>();

        if (pluginId == uint.MaxValue)
            return uint.MaxValue; // Plugin ID == -1 (invalid)

        // var type = // (pluginId >> 0) & 0x000F
        // var company = // (pluginId >> 4) & 0x03FF

        return pluginId;
    }
}
