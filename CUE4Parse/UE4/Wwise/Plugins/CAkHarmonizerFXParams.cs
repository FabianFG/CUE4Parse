using System;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkHarmonizerFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkHarmonizerFXParams Params = new(Ar);
}

internal struct AkHarmonizerFXParams
{
    public AkPitchVoiceParams[] Voice;
    public AkInputType InputType;
    public float DryLevel;
    public float WetLevel;
    public uint WindowSize;
    public bool ProcessLFE;
    public bool SyncDry;

    public AkHarmonizerFXParams(FWwiseArchive Ar)
    {
        Voice = Ar.ReadArray(2, () => new AkPitchVoiceParams(Ar));

        InputType = Ar.Read<AkInputType>();
        DryLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        WetLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        WindowSize = Ar.Read<uint>();
        ProcessLFE = Ar.Read<byte>() != 0;
        SyncDry = Ar.Read<byte>() != 0;
    }
}
