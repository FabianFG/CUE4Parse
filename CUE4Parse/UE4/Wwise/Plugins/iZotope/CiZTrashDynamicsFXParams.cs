using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashDynamicsFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public iZTrashDynamicsFXParams Params = new(Ar);
}

public struct iZTrashDynamicsFXParams(FWwiseArchive Ar)
{
    public iZTrashDynamicsRTPCParams RTPC = Ar.Read<iZTrashDynamicsRTPCParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZTrashDynamicsRTPCParams
{
    public uint uBypass;
    public float fCompressorThreshold;
    public float fCompressorRatio;
    public float fCompressorAttack;
    public float fCompressorRelease;
    public float fGateThreshold;
    public float fGateRatio;
    public float fGateAttack;
    public float fGateRelease;
    public float fGain;
}
