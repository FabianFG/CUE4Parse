using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Plugins;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwisePlugin
{
    // TODO: add all plugins
    private static readonly Dictionary<uint, Action<FArchive, uint, uint>> PluginDispatch = new()
    {
        //{ 0x00640002, CAkFxSrcSine },
        { 0x00650002, CAkFxSrcSilence.Read },
        //{ 0x00660002, CAkToneGen },
        //{ 0x00690003, CAkParameterEQFX },
        //{ 0x006A0003, CAkDelayFX },
        //{ 0x006E0003, CAkPeakLimiterFX },
        //{ 0x00730003, CAkFDNReverbFX },
        //{ 0x00760003, CAkRoomVerbFX },
        //{ 0x007D0003, CAkFlangerFX },
        //{ 0x007E0003, CAkGuitarDistortionFX },
        //{ 0x007F0003, CAkConvolutionReverbFX },
        //{ 0x00810003, CAkMeterFX },
        //{ 0x00870003, CAkStereoDelayFX },
        //{ 0x008B0003, CAkGainFX },
        //{ 0x008A0003, CAkHarmonizerFX },
        //{ 0x00940002, CAkSynthOne },
        //{ 0x00C80002, CAkFxSrcAudioInput },
        //{ 0x00041033, iZTrashDelayFXParams }
    };

    public static void ParsePluginParams(FArchive Ar, uint pluginId, bool always=false)
    {
        if (pluginId == 0)
            return;

        if ((int) pluginId < 0 && !always)
            return;

        uint size = Ar.Read<uint>();
        if (size == 0)
            return;

        if (PluginDispatch.TryGetValue(pluginId, out var dispatch))
        {
            dispatch(Ar, size, pluginId);
        }
        else
        {
            SkipPlugin(Ar, size, pluginId);
        }
    }

    public static uint ParsePlugin(FArchive Ar)
    {
        var fxId = Ar.Read<uint>();
        if (fxId == uint.MaxValue)
        {
            return uint.MaxValue; // Plugin ID == -1 (invalid)
        }

        //var type = // (fld >> 0) & 0x000F
        //var company = // (fld >> 4) & 0x03FF

        return fxId;
    }

    private static void SkipPlugin(FArchive Ar, uint size, uint pluginId)
    {
        Log.Warning($"Handler for Wwise plugin '{pluginId}' wasn't added, skipping");
        Ar.Position += size; // Skip
    }

    public static void EnsureEndOfBlock(FArchive Ar, long endPosition, uint pluginId)
    {
        if (Ar.Position != endPosition)
        {
            Log.Warning($"Didn't read Wwise plugin '{pluginId}' correctly (at {Ar.Position}, should be {endPosition})");
            Ar.Position = endPosition;
        }
    }
}
