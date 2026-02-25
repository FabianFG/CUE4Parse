using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkReflectFXParams(FArchive Ar) : IAkPluginParam
{
    public AkReflectFXParams Params = new AkReflectFXParams(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkReflectFXParams
{
    public float CenterPerc;
    public float MaxReflections;
    public float DryGain;
    public float WetGainDB;
    public CAkConversionTable[] m_Curves;
    public float MaxDistance;
    public float BaseTextureFrequency;
    public uint uFadeOutNbFrames;
    public AkFilteredFracDelayLineParams delayLineParams;
    public float fPrevDryGain;
    public AkChannelConfig OutputChannelConfig;
    public float DistanceWarping;
    public float DiffractionWarping;
    public AkDecorrParams DecorrParams;
    public float FadeTime;

    public AkReflectFXParams(FArchive Ar)
    {
        delayLineParams.SpeedOfSound = Math.Max(Ar.Read<float>(), 0.001f);
        CenterPerc = Ar.Read<float>();
        MaxReflections = Ar.Read<float>();
        DryGain = Ar.Read<float>();
        WetGainDB = Ar.Read<float>();
        MaxDistance = Ar.Read<float>();
        BaseTextureFrequency = Ar.Read<float>();
        uFadeOutNbFrames = Ar.Read<uint>();
        delayLineParams.ParamFilterCutoff = Ar.Read<float>();
        delayLineParams.ParamFilterType = Ar.Read<uint>();
        delayLineParams.PitchThreshold = Ar.Read<float>();
        delayLineParams.DistanceThreshold = Ar.Read<float>();
        delayLineParams.ThresholdMode = Ar.Read<uint>();
        if (WwiseVersions.Version >= 145)
        {
            DistanceWarping = Ar.Read<float>();
            DiffractionWarping = Ar.Read<float>();
        }
        OutputChannelConfig = Ar.Read<AkChannelConfig>();
        if (WwiseVersions.Version >= 145)
        {
            DecorrParams = new AkDecorrParams(Ar);
            FadeTime = Ar.Read<float>();
        }
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
    public float SpeedOfSound;
    public float ParamFilterCutoff;
    public uint ParamFilterType;
    public float PitchThreshold;
    public float DistanceThreshold;
    public uint ThresholdMode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkDecorrParams(FArchive Ar)
{
    public float FusingTime = Ar.Read<float>();
    public float DecorrStrength = Ar.Read<float>();
    public int DecorrAlgorithmSelect = Ar.Read<int>();
    public int DecorrStrengthSource = Ar.Read<int>();
    public uint DecorrFilterMaxReflectionOrder = Ar.Read<uint>();
    public bool StereoDecorrelation = Ar.Read<byte>() != 0;
    public float DecorrWindowWidth = Ar.Read<float>();
    public bool DecorrHardwareAcceleration = Ar.Read<byte>() != 0;
    public uint MaterialFilteringSelect = Ar.Read<uint>();
}
