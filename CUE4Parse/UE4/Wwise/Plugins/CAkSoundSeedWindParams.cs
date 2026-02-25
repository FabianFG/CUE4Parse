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
        WindParams.MaxDistance = Ar.Read<float>();
        m_pDeflectors = Ar.ReadArray(deflectorCount, () => new AkWindDeflectorParams(Ar));
        m_Curves = Ar.ReadArray(Ar.Read<ushort>(), () => new CAkConversionTable(Ar, false));
    }
}

public struct AkWindParams
{
    public float Duration;
    public float DurationRandom;
    public uint ChannelMask;
    public float MinDistance;
    public float AttenuationRolloff;
    public float MaxDistance;
    public float DynamicRange;
    public float PlaybackRate;
    public float[] fBaseValue;
    public float[] fRandomValue;
    public bool[] bAutomation;
    public AkWindDeflectorParams[] m_pDeflectors;
    public CAkConversionTable[] m_Curves;

    public AkWindParams(FArchive Ar)
    {
        Duration = Ar.Read<float>();
        DurationRandom = Ar.Read<float>();
        var value = Ar.Read<ushort>();
        ChannelMask = value switch
        {
             0 => 4,
             2 => 0x603,
             _ => value,
        };

        MinDistance = Ar.Read<float>();
        AttenuationRolloff = Ar.Read<float>();
        DynamicRange = Ar.Read<float>();
        PlaybackRate = Ar.Read<float>();
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
    public float Distance = Ar.Read<float>();
    public float Angle = Ar.Read<float>();
    public float Frequency = Ar.Read<float>();
    public float QFactor = Ar.Read<float>();
    public float Gain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
}
