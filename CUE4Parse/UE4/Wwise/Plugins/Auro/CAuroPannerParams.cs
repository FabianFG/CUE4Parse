using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.Auro;

public class CAuroPannerMixerParams(FArchive Ar) : IAkPluginParam
{
    public bool EnableDefaultSpatialization = Ar.Read<byte>() != 0;
    public float PanningLawdB = Ar.Read<float>();
}

public class CAuroPannerFXParams(FArchive Ar) : IAkPluginParam
{
    public bool EnableCustomObjectSpread = Ar.Read<byte>() != 0;
    public float ObjectSpreadX = Ar.Read<float>();
    public float ObjectSpreadY = Ar.Read<float>();
    public float ObjectSpreadZ = Ar.Read<float>();
    public float CenterFactorFC = Ar.Read<float>();
    public float CenterFactorHC = Ar.Read<float>();
    public float CenterFactorT = Ar.Read<float>();
    public bool EnableDownfoldSettings = Ar.Read<byte>() != 0;
    public float DownfoldGainH = Ar.Read<float>();
    public float DownfoldGainT = Ar.Read<float>();
    public float DownfoldTopChannel = Ar.Read<float>();
    public bool PanningMode = Ar.Read<byte>() != 0;
    public float ZoomFactor = Ar.Read<float>();
    public float ZoomAzimuth = Ar.Read<float>();
    public float ZoomElevation = Ar.Read<float>();
}
