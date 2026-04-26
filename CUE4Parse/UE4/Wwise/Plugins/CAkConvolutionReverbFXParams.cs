using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkConvolutionReverbFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkConvolutionReverbParams Params = new(Ar);
}

public struct AkConvolutionReverbParams
{
    public float fPreDelay;
    public float fFrontRearDelay;
    public float fStereoWidth;
    public float fInputCenterLevel;
    public float fInputLFELevel;
    public float fInputStereoWidth;
    public float fFrontLevel;
    public float fRearLevel;
    public float fCenterLevel;
    public float fLFELevel;
    public float fDryLevel;
    public float fWetLevel;
    public AkConvolutionAlgoType eAlgoType;
    public float fInputThreshold;
    public byte unknown;

    public AkConvolutionReverbParams(FWwiseArchive Ar)
    {
        fPreDelay = Ar.Read<float>();
        fFrontRearDelay = Ar.Read<float>();
        fStereoWidth = Ar.Read<float>();
        fInputCenterLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fInputLFELevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fInputStereoWidth = Ar.Version >= 120 ? Ar.Read<float>() : 0;
        fFrontLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fRearLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fCenterLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fLFELevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fDryLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fWetLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        eAlgoType = Ar.Read<AkConvolutionAlgoType>();
        fInputThreshold = Ar.Version >= 135 ? MathF.Pow(10f, Ar.Read<float>() * 0.05f) : 0;
        unknown = Ar.Version >= 145 ? Ar.Read<byte>() : (byte)0;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkConvolutionAlgoType : uint
{
    AKCONVALGOTYPE_DOWNMIX = 0x0,
    AKCONVALGOTYPE_DIRECT = 0x1
}
