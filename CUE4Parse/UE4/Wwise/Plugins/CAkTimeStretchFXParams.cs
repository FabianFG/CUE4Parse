using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkTimeStretchFXParams(FArchive Ar) : IAkPluginParam
{
    public AkTimeStretchFXParams Params = new AkTimeStretchFXParams(Ar);
}

public struct AkTimeStretchFXParams
{
    public uint uWindowSize;
    public float fTimeStretch;
    public float fTimeStretchRandom;
    public float fPitchShift;
    public float fPitchShiftRandom;
    public float fOutputGain;
    public float fTolerance;
    public StereoProcType iStereoProc;

    public AkTimeStretchFXParams(FArchive Ar)
    {
        uWindowSize = Ar.Read<uint>();
        fTimeStretch = Ar.Read<float>();
        fTimeStretchRandom = Ar.Read<float>();
        if (WwiseVersions.Version < 145)
        {
            fOutputGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        }
        else
        {
            fPitchShift = Ar.Read<float>();
            fPitchShiftRandom = Ar.Read<float>();
            fOutputGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
            fTolerance = MathF.Max(MathF.Exp(Ar.Read<float>() * -0.0599999987f) - 0.00247884f, 0.0001f);
            iStereoProc = Ar.Read<StereoProcType>();
        }
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum StereoProcType : int
{
    Stereo = 0x0,
    CenterCut = 0x1
};
