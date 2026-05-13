using System;
using CUE4Parse.UE4.Wwise.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkSoundSeedWooshParams : IAkPluginParam
{
    public AkWooshParams WooshParams;
    public AkWooshDeflectorParams[] Deflectors;
    public CAkConversionTable[] Curves;
    public float TotalPathDistance;
    public AkWooshPathPoint[] Path;

    public CAkSoundSeedWooshParams(FWwiseArchive Ar)
    {
        WooshParams = new AkWooshParams(Ar);
        Deflectors = Ar.ReadArray(Ar.Read<ushort>(), () => new AkWooshDeflectorParams(Ar));
        Curves = Ar.ReadArray(Ar.Read<ushort>(), () => { Ar.Read<int>(); return new CAkConversionTable(Ar, false); });
        var pointsNum = Ar.Read<ushort>();
        TotalPathDistance = Ar.Read<float>();
        Path = Ar.ReadArray<AkWooshPathPoint>(pointsNum);
    }
}

public struct AkWooshParams
{
    public float Duration;
    public float DurationRdm;
    public uint ChannelMask;
    public float MinDistance;
    public float AttenuationRolloff;
    public float DynamicRange;
    public float PlaybackRate;
    public int AnchorIndex;
    public EAkNoiseColor NoiseColor;
    public float RandomSpeedX;
    public float RandomSpeedY;
    public uint OversamplingFactor;

    public FSoundSeedParamvalue[] Values;
    public byte bEnableDistanceBasedAttenuation;

    public AkWooshParams(FWwiseArchive Ar)
    {
        Duration = Ar.Read<float>();
        DurationRdm = Ar.Read<float>();
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
        NoiseColor = Ar.Read<EAkNoiseColor>();
        RandomSpeedX = Ar.Read<float>();
        RandomSpeedY = Ar.Read<float>();
        bEnableDistanceBasedAttenuation = Ar.Read<byte>();
        OversamplingFactor = Ar.Read<ushort>();
        Values = Ar.ReadArray(4, () => new FSoundSeedParamvalue(Ar));
        AnchorIndex = Ar.Read<short>();
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

public struct AkWooshDeflectorParams(FWwiseArchive Ar)
{
    public float Frequency = Ar.Read<float>();
    public float QFactor = Ar.Read<float>();
    public float Gain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
};

public struct AkWooshPathPoint()
{
    public float DistanceTravelled;
    public float X;
    public float Y;
}
