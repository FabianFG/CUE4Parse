using System;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkSidechainSendFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkSidechainSendFXParams Params = new(Ar);
}

public struct AkSidechainSendFXParams
{
    public AkSidechainRTPCParams RTPC;
    public AkSidechainNonRTPCParams NonRTPC;

    public AkSidechainSendFXParams(FWwiseArchive Ar)
    {
        NonRTPC.SidechainId = Ar.Read<uint>();
        NonRTPC.SidechainScope = Ar.Read<int>();
        RTPC.Volume = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        RTPC.LpfFactor = Ar.Read<short>();
        RTPC.HpfFactor = Ar.Read<short>();
        NonRTPC.bDelayOutput = Ar.Read<byte>() != 0;
    }
}

public class CAkSidechainRecvFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkSidechainRecvFXParams Params = new(Ar);
}

public struct AkSidechainRecvFXParams
{
    public AkSidechainRTPCParams RTPC;
    public AkSidechainNonRTPCParams NonRTPC;

    public AkSidechainRecvFXParams(FWwiseArchive Ar)
    {
        NonRTPC.SidechainId = Ar.Read<uint>();
        NonRTPC.SidechainScope = Ar.Read<int>();
        RTPC.Volume = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        RTPC.LpfFactor = Ar.Read<short>();
        RTPC.HpfFactor = Ar.Read<short>();
    }
}

public struct AkSidechainRTPCParams
{
    public float Volume;
    public short LpfFactor;
    public short HpfFactor;
}
public struct AkSidechainNonRTPCParams
{
    public uint SidechainId;
    public int SidechainScope;
    public bool bDelayOutput;
}
