using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.Auro;

public class CAuroHPFXParams(FArchive Ar) : IAkPluginParam
{
    public AuroHPFXParams Params = new AuroHPFXParams(Ar);
}

public struct AuroHPFXParams(FArchive Ar)
{
    public float[] fParams = Ar.ReadArray<float>(17);
    public bool bBypass = Ar.Read<byte>() != 0;
    public bool bEnableReverb = Ar.Read<byte>() != 0;
}
