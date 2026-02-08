using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkMotionGeneratorParams(FArchive Ar) : IAkPluginParam
{
    public AkMotionGeneratorParams Params = new AkMotionGeneratorParams(Ar);
}

public struct AkMotionGeneratorParams
{
    public float m_fPeriod;
    public float m_fPeriodMultiplier;
    public float m_fDuration;
    public float m_fAttackTime;
    public float m_fDecayTime; 
    public float m_fSustainTime;
    public float m_fReleaseTime;
    public float m_fSustainLevel;

    public ushort m_eDurationType;
    public CAkConversionTable[] m_Curves;

    public AkMotionGeneratorParams(FArchive Ar)
    {
        m_fPeriod = Ar.Read<float>();
        m_fPeriodMultiplier = Ar.Read<float>();
        m_fDuration = Ar.Read<float>();
        m_fAttackTime = Ar.Read<float>();
        m_fDecayTime = Ar.Read<float>();
        m_fSustainTime = Ar.Read<float>();
        m_fReleaseTime = Ar.Read<float>();
        m_fSustainLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        m_eDurationType = Ar.Read<ushort>();
        m_Curves = Ar.ReadArray(Ar.Read<ushort>(), () => new CAkConversionTable(Ar, false));
    }
}
