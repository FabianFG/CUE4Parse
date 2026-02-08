using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkReflectFXParams(FArchive Ar) : IAkPluginParam
{
    public AkReflectFXParams Params = new AkReflectFXParams(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkReflectFXParams
{
    public float fCenterPerc;
    public float fMaxReflections;
    public float fDryGain;
    public float fWetGainDB;
    public CAkConversionTable[] m_Curves;
    public float fMaxDistance;
    public float fBaseTextureFreq;
    public uint uFadeOutNbFrames;
    public AkFilteredFracDelayLineParams delayLineParams;
    public float fPrevDryGain;
    public AkChannelConfig outputConfig;

    public AkReflectFXParams(FArchive Ar)
    {
        delayLineParams.fSpeedOfSound = Math.Max(Ar.Read<float>(), 0.001f);
        fCenterPerc = Ar.Read<float>();
        fMaxReflections = Ar.Read<float>();
        fDryGain = Ar.Read<float>();
        fWetGainDB = Ar.Read<float>();
        fMaxDistance = Ar.Read<float>();
        fBaseTextureFreq = Ar.Read<float>();
        uFadeOutNbFrames = Ar.Read<uint>();
        delayLineParams.fParamFilterCutoff = Ar.Read<float>();
        delayLineParams.uParamFilterIsFIR = Ar.Read<uint>();
        delayLineParams.fPitchThreshold = Ar.Read<float>();
        delayLineParams.fDistanceThreshold = Ar.Read<float>();
        delayLineParams.uThresholdMode = Ar.Read<uint>();
        if (WwiseVersions.Version >= 154)
            Ar.Position += 8;
        outputConfig = Ar.Read<AkChannelConfig>();
        if (WwiseVersions.Version >= 154)
            Ar.Position += 34;
        var curvesCount = Ar.Read<ushort>();
        m_Curves = new CAkConversionTable[curvesCount];
        // scaling depends on index and wwise version
        for (var i = 0; i < curvesCount; i++)
        {
            m_Curves[Ar.Read<int>()] = new CAkConversionTable(Ar, false);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkFilteredFracDelayLineParams
{
    public float fSpeedOfSound;
    public float fParamFilterCutoff;
    public uint uParamFilterIsFIR;
    public float fPitchThreshold;
    public float fDistanceThreshold;
    public uint uThresholdMode;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkChannelConfig : uint
{
    SameasAudioDevice = 0,
    SameasMainMix = 3584,
    SameasPassThroughMix = 3840,
    AudioObjects = 768,
    Audio_1_0 = 16641,
    Audio_2_0 = 12546,
    Audio_2_1 = 45315,
    Audio_3_0 = 28931,
    Audio_4_0 = 6304004,
    Audio_5_1 = 6353158,
    Audio_7_1 = 6549768,
    Audio_5_1_2 = 90239240,
    Audio_5_1_4 = 761327882,
    Audio_7_1_2 = 90435850,
    Audio_7_1_4 = 761524492,
    Ambisonics1storder = 516,
    Ambisonics2ndorder = 521,
    Ambisonics3rdorder = 528,
    Ambisonics4thorder = 537,
    Ambisonics5thorder = 548,
    Auro10_1 = 769716491,
    Auro11_1 = 803270924,
    Auro13_1 = 803467534,
    LFE = 33025,
}
