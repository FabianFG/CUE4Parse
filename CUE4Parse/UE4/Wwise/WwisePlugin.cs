using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Plugins;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwisePlugin
{
    // TODO: add all plugins
    private static readonly Dictionary<EAkPluginId, Func<FArchive, uint, EAkPluginId, object>> _pluginDispatch = new()
    {
        //{ EAkPluginId.AkFxSrcSine, CAkFxSrcSine.Read },
        { EAkPluginId.AkFxSrcSilence, CAkFxSrcSilence.Read },
        //{ EAkPluginId.AkToneGen, CAkToneGen.Read },
        //{ EAkPluginId.AkParameterEQFX, CAkParameterEQFX.Read },
        //{ EAkPluginId.AkDelayFX, CAkDelayFX.Read },
        //{ EAkPluginId.AkCompressor, CAkCompressor.Read },
        //{ EAkPluginId.AkPeakLimiterFX, CAkPeakLimiterFX.Read },
        //{ EAkPluginId.AkFDNReverbFX, CAkFDNReverbFX.Read },
        //{ EAkPluginId.AkRoomVerbFX, CAkRoomVerbFX.Read },
        //{ EAkPluginId.AkFlangerFX, CAkFlangerFX.Read },
        //{ EAkPluginId.AkGuitarDistortionFX, CAkGuitarDistortionFX.Read },
        //{ EAkPluginId.AkConvolutionReverbFX, CAkConvolutionReverbFX.Read },
        //{ EAkPluginId.AkMeterFX, CAkMeterFX.Read },
        //{ EAkPluginId.AkTremolo, CAkTremolo.Read },
        //{ EAkPluginId.AkStereoDelayFX, CAkStereoDelayFX.Read },
        //{ EAkPluginId.AkPitchShifter, CAkPitchShifter.Read },
        //{ EAkPluginId.AkGainFX, CAkGainFX.Read },
        //{ EAkPluginId.AkHarmonizerFX, CAkHarmonizerFX.Read },
        //{ EAkPluginId.AkSynthOne, CAkSynthOne.Read },
        //{ EAkPluginId.AkFxSrcAudioInput, CAkFxSrcAudioInput.Read },
        //{ EAkPluginId.AkMotion, CAkMotion.Read },
        //{ EAkPluginId.iZTrashDelayFXParams, iZTrashDelayFXParams.Read }
    };

    public static object? TryParsePluginParams(FArchive Ar, EAkPluginId pluginId, bool always = false)
    {
        if (pluginId is EAkPluginId.None)
            return null;
        if ((int) pluginId < 0 && !always)
            return null;

        uint size = Ar.Read<uint>();
        if (size == 0)
            return null;

        if (_pluginDispatch.TryGetValue(pluginId, out var dispatch))
            return dispatch;

        SkipPlugin(Ar, size, pluginId);
        return null;
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

    private static void SkipPlugin(FArchive Ar, uint size, EAkPluginId pluginId)
    {
#if DEBUG
        Log.Warning($"Handler for Wwise plugin '{pluginId}' wasn't added, skipping");
#endif
        Ar.Position += size; // Skip
    }

    public static void EnsureEndOfBlock(FArchive Ar, long endPosition, EAkPluginId pluginId)
    {
        if (Ar.Position != endPosition)
        {
            Log.Warning($"Didn't read Wwise plugin '{pluginId}' correctly (at {Ar.Position}, should be {endPosition})");
            Ar.Position = endPosition;
        }
    }
}
