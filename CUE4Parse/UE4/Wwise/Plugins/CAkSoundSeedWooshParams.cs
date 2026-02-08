using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkSoundSeedWooshParams : IAkPluginParam
{
    public AkWooshParams WooshParams;
    public AkWooshDeflectorParams[] m_pDeflectors;
    public CAkConversionTable[] m_Curves;
    public AkWooshPathPoint[] m_pPath;

    public CAkSoundSeedWooshParams(FArchive Ar)
    {
        WooshParams = new AkWooshParams(Ar);
        m_pDeflectors = Ar.ReadArray(Ar.Read<ushort>(), () => new AkWooshDeflectorParams(Ar));
        m_Curves = Ar.ReadArray(Ar.Read<ushort>(), () => new CAkConversionTable(Ar, false));
        m_pPath = Ar.ReadArray<AkWooshPathPoint>(Ar.Read<ushort>());
    }
}

public struct AkWooshParams
{
    public float fDuration;
    public float fDurationRdm;
    public uint uChannelMask;
    public float fMinDistance;
    public float fAttenuationRolloff;
    public float fDynamicRange;
    public float fPlaybackRate;
    public int iAnchorIndex;
    public EAkNoiseColor eNoiseColor;
    public float fRandomSpeedX;
    public float fRandomSpeedY;
    public uint uOversamplingFactor;

    public float[] fBaseValue;
    public float[] fRandomValue;
    public bool[] bAutomation;
    public byte bEnableDistanceBasedAttenuation;

    public AkWooshParams(FArchive Ar)
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
        iAnchorIndex = Ar.Read<int>();
        eNoiseColor = Ar.Read<EAkNoiseColor>();
        fRandomSpeedX = Ar.Read<float>();
        fRandomSpeedY = Ar.Read<float>();
        bEnableDistanceBasedAttenuation = Ar.Read<byte>();
        uOversamplingFactor = Ar.Read<ushort>();
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

        iAnchorIndex = Ar.Read<int>();
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EAkNoiseColor : ushort
{
    NOISECOLOR_WHITE = 0,
    NOISECOLOR_PINK = 1,
    NOISECOLOR_RED = 2,
    NOISECOLOR_PURPLE = 3
}

public struct AkWooshDeflectorParams(FArchive Ar)
{
    public float fFrequency = Ar.Read<float>();
    public float fQFactor = Ar.Read<float>();
    public float fGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
};

public struct AkWooshPathPoint(FArchive Ar)
{
    public float fDistanceTravelled;
    public float fX;
    public float fY;
}
