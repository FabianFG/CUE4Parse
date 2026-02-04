using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkModalSynthParams : IAkPluginParam
{
    public AkModalSynthParams Params;
    public float m_fGlobalGain;
    public AkModalSynthMode[] m_pModes;

    public CAkModalSynthParams(FArchive Ar)
    {
        Params = new AkModalSynthParams(Ar);
        m_fGlobalGain = Ar.Read<float>();
        m_pModes = Ar.ReadArray<AkModalSynthMode>(Ar.Read<ushort>());
    }
}

public struct AkModalSynthParams
{
    public float fResidualLevel;
    public float fOutputLevel;
    public float fFreqAmt;
    public float fFreqVar;
    public float fBWAmt;
    public float fBWVar;
    public float fMagVar;
    public float fModelQuality;

    public bool bFreqEnable;
    public bool bBWEnable;
    public bool bMagEnable;

    public AkModalSynthParams(FArchive Ar)
    {
        fResidualLevel = (float) System.Math.Pow(10f, Ar.Read<float>() * 0.05);
        fOutputLevel = (float) System.Math.Pow(10f, Ar.Read<float>() * 0.05);
        fFreqAmt = Ar.Read<float>();
        fFreqVar = Ar.Read<float>();
        fBWAmt = Ar.Read<float>();
        fBWVar = Ar.Read<float>();
        fMagVar = Ar.Read<float>();
        fModelQuality = Ar.Read<float>();
        bFreqEnable = Ar.Read<byte>() != 0;
        bBWEnable = Ar.Read<byte>() != 0;
        bMagEnable = Ar.Read<byte>() != 0;
    }
}

public struct AkModalSynthMode
{
    public float fFreq;
    public float fMag;
    public float fBW;
}
