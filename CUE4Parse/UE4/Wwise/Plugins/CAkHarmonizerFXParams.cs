using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkHarmonizerFXParams(FArchive Ar) : IAkPluginParam
{
    public AkHarmonizerFXParams Params = new AkHarmonizerFXParams(Ar);
}

internal struct AkHarmonizerFXParams
{
    public AkPitchVoiceParams[] Voice;
    public AkInputType eInputType;
    public float fDryLevel;
    public float fWetLevel;
    public uint uWindowSize;
    public bool bProcessLFE;
    public bool bSyncDry;

    public AkHarmonizerFXParams(FArchive Ar)
    {
        Voice = Ar.ReadArray(2, () => new AkPitchVoiceParams(Ar));

        eInputType = Ar.Read<AkInputType>();
        fDryLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fWetLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        uWindowSize = Ar.Read<uint>();
        bProcessLFE = Ar.Read<byte>() != 0;
        bSyncDry = Ar.Read<byte>() != 0;
    }
}
