using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashDelayFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public iZTrashDelayFXParams Params = new(Ar);
}

public struct iZTrashDelayFXParams(FWwiseArchive Ar)
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

