using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashDelayFXParams(FArchive Ar) : IAkPluginParam
{
    public iZTrashDelayFXParams Params = new iZTrashDelayFXParams(Ar);
}

public struct iZTrashDelayFXParams(FArchive Ar)
{
    public iZTrashDelayRTPCParams RTPC = Ar.Read<iZTrashDelayRTPCParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZTrashDelayRTPCParams
{
    public float fDryOut;
    public float fWetOut;
    public float fLowCutoff;
    public float fLowQ;
    public float fHighCutoff;
    public float fHighQ;
    public float fAmount;
    public float fFeedback;
    public float fTrash;
    public uint uDelayType;
}

