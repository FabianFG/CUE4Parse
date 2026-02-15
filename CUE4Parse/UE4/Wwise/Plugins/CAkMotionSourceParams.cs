using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkMotionSourceParams(FArchive Ar) : IAkPluginParam
{
    public AkMotionSourceParams Params = new AkMotionSourceParams(Ar);
}

public struct AkMotionSourceParams
{
    public float m_fChannel1;
    public float m_fChannel2;
    public float m_fChannel3;
    public float m_fChannel4;
    public float m_fChannel5;
    public float m_fChannel6;
    public float m_fChannel7;
    public float m_fChannel8;
    public byte m_uNumCurves;
    public EAkMotionSourceCurveType m_uCurveType;
    public ushort[] m_uAssigns;

    public AkMotionSourceParams(FArchive Ar)
    {
        m_fChannel1 = Ar.Read<float>();
        m_fChannel2 = Ar.Read<float>();
        m_fChannel3 = Ar.Read<float>();
        m_fChannel4 = Ar.Read<float>();
        m_fChannel5 = Ar.Read<float>();
        m_fChannel6 = Ar.Read<float>();
        m_fChannel7 = Ar.Read<float>();
        m_fChannel8 = Ar.Read<float>();
        m_uNumCurves = Ar.Read<byte>();
        m_uCurveType = Ar.Read<EAkMotionSourceCurveType>();
        m_uAssigns = Ar.ReadArray<ushort>(m_uNumCurves);
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EAkMotionSourceCurveType : byte
{
    Amplitude = 0x00,
    HapticsWave = 0x01
}
