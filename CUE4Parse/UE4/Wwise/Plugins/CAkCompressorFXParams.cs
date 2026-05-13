using System;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkCompressorFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkCompressorFXParams Params = new(Ar);
}

public struct AkCompressorFXParams(FWwiseArchive Ar)
{
    public float Threshold = Ar.Read<float>();
    public float Ratio = Ar.Read<float>();
    public float Attack = Ar.Read<float>();
    public float Release = Ar.Read<float>();
    public float OutputLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
    public float ChannelLinkPercentage = Ar.Version >= 172 ? Ar.Read<float>() : 0;
    public bool ProcessLFE = Ar.Read<byte>() != 0;
    public bool ChannelLink = Ar.Read<byte>() != 0;
    public bool SidechainGlobalScope = Ar.Version >= 172 && Ar.Read<byte>() != 0;
    public uint SidechainId = Ar.Version >= 172 ? Ar.Read<uint>() : 0;

}
