using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;

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
