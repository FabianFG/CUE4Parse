using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashDynamicsFXParams(FArchive Ar) : IAkPluginParam
{
    public iZTrashDynamicsFXParams Params = new iZTrashDynamicsFXParams(Ar);
}

public struct iZTrashDynamicsFXParams(FArchive Ar)
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
