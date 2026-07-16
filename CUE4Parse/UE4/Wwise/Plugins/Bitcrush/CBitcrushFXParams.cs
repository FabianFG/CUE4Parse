namespace CUE4Parse.UE4.Wwise.Plugins.Bitcrush;

public class CBitcrushFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public float InputAmplitude = Ar.Read<float>();
    public float OutputAmplitude = Ar.Read<float>();
    public float BitRate = Ar.Read<float>();
    public float SampleRate = Ar.Read<float>();
    public bool ClipType = Ar.ReadBool();
    public float Drive = Ar.Read<float>();
}
