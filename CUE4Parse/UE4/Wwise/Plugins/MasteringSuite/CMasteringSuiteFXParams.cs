using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins.MasteringSuite;

public class CMasteringSuiteFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public MasteringSuiteFXParams Params = new MasteringSuiteFXParams(Ar);
}

public struct MasteringSuiteFXParams(FWwiseArchive Ar)
{
    public bool[] moduleBypassFlags = Ar.ReadArray(4, () => Ar.Read<byte>() != 0);
    public SceAudioOut2MasteringParamEqParamsV2 paramEqParams = new SceAudioOut2MasteringParamEqParamsV2(Ar);
    public SceAudioOut2MasteringCompressorParamsV2 compressorParams = new SceAudioOut2MasteringCompressorParamsV2(Ar);
    public float[] masterVolumeParams = Ar.ReadArray(12, Ar.Read<float>);
    public SceAudioOut2MasteringLimiterParamsV2 limiterParams = new SceAudioOut2MasteringLimiterParamsV2(Ar);
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceAudioOut2MasteringParamEqFilterParams
{
    public uint m_eqBandsFilterMode;
    public float frequency;
    public float gain;
    public float resonance;
};

public struct SceAudioOut2MasteringParamEqParamsV2(FWwiseArchive Ar)
{
    public uint numBands = Ar.Read<uint>();
    public bool[] m_eqBandsBypassFlags = Ar.ReadArray(6, () => Ar.Read<byte>() != 0);
    public SceAudioOut2MasteringParamEqFilterParams[] filterParams = Ar.ReadArray<SceAudioOut2MasteringParamEqFilterParams>(6);
};

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceAudioOut2MasteringCompressorBandParams
{
    public float threshold;
    public float ratio;
    public float attack;
    public float release;
    public float makeupGain;
    public float knee;
};

public struct SceAudioOut2MasteringCompressorParamsV2(FWwiseArchive Ar)
{
    public uint numBands = Ar.Read<uint>();
    public uint linkMode = Ar.Read<uint>();
    public float linkStrength = Ar.Read<float>();
    public bool linkStereoPairs = Ar.Read<byte>() != 0;
    public bool[] bandsBypassFlags = Ar.ReadArray(4, () => Ar.Read<byte>() != 0);
    public float[] crossoverFrequencies = Ar.ReadArray(3, Ar.Read<float>);
    public SceAudioOut2MasteringCompressorBandParams[] bandParams = Ar.ReadArray<SceAudioOut2MasteringCompressorBandParams>(4);
};

public struct SceAudioOut2MasteringLimiterParamsV2(FWwiseArchive Ar)
{
    public uint mode = Ar.Read<uint>();
    public float threshold = Ar.Read<float>();
    public float attack = Ar.Read<float>();
    public float release = Ar.Read<float>();
    public float outputGain = Ar.Read<float>();
    public bool linkMOde = Ar.Read<byte>() != 0;
};


