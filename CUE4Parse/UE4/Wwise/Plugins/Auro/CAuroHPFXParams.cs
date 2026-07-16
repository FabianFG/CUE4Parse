namespace CUE4Parse.UE4.Wwise.Plugins.Auro;

public class CAuroHPFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AuroHPFXParams Params = new(Ar);
}

public struct AuroHPFXParams(FWwiseArchive Ar)
{
    public float[] fParams = Ar.ReadArray<float>(17);
    public bool bBypass = Ar.Read<byte>() != 0;
    public bool bEnableReverb = Ar.Read<byte>() != 0;
}
