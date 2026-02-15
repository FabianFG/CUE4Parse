using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkSoundSeedWindParams : IAkPluginParam
{
    public AkWindParams WindParams;
    public AkWindDeflectorParams[] m_pDeflectors;
    public CAkConversionTable[] m_Curves;

    public CAkSoundSeedWindParams(FArchive Ar)
    {
        WindParams = new AkWindParams(Ar);
        var deflectorCount = Ar.Read<ushort>();
        WindParams.fMaxDistance = Ar.Read<float>();
        m_pDeflectors = Ar.ReadArray(() => new AkWindDeflectorParams(Ar));
        m_Curves = Ar.ReadArray(() => new CAkConversionTable(Ar, false));
    }
}

public struct AkWindParams
{
    public float fDuration;
    public float fDurationRdm;
    public uint uChannelMask;
    public float fMinDistance;
    public float fAttenuationRolloff;
    public float fMaxDistance;
    public float fDynamicRange;
    public float fPlaybackRate;
    public float[] fBaseValue;
    public float[] fRandomValue;
    public bool[] bAutomation;
    public AkWindDeflectorParams[] m_pDeflectors;
    public CAkConversionTable[] m_Curves;

    public AkWindParams(FArchive Ar)
    {
        fDuration = Ar.Read<float>();
        fDurationRdm = Ar.Read<float>();
        var value = Ar.Read<ushort>();
        uChannelMask = value switch
        {
             0 => 4,
             2 => 0x603,
             _ => value,
        };

        fMinDistance = Ar.Read<float>();
        fAttenuationRolloff = Ar.Read<float>();
        fDynamicRange = Ar.Read<float>();
        fPlaybackRate = Ar.Read<float>();
        int channelCount = 7;
        fBaseValue = new float[channelCount];
        fRandomValue = new float[channelCount];
        bAutomation = new bool[channelCount];
        for (int i = 0; i < channelCount; i++)
        {
            fBaseValue[i] = Ar.Read<float>();
            fRandomValue[i] = Ar.Read<float>();
            bAutomation[i] = Ar.Read<bool>();
        }
    }
}

public struct AkWindDeflectorParams(FArchive Ar)
{
    public float fDistance = Ar.Read<float>();
    public float fAngle = Ar.Read<float>();
    public float fFrequency = Ar.Read<float>();
    public float fQFactor = Ar.Read<float>();
    public float fGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
}
