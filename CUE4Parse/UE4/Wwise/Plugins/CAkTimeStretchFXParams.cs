using System;
using CUE4Parse.UE4.Readers;

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
    public float fOutputGain;

    public AkTimeStretchFXParams(FArchive Ar)
    {
        uWindowSize = Ar.Read<uint>();
        fTimeStretch = Ar.Read<float>();
        fTimeStretchRandom = Ar.Read<float>();
        if (WwiseVersions.Version < 154)
        {
            fOutputGain = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        }
        else
        {
            Ar.Position += 8;
            fOutputGain = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
            Ar.Position += 8;
        }
    }
}
