using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashBoxModelerFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public iZTrashBoxModelerFXParams Params = new(Ar);
}

public struct iZTrashBoxModelerFXParams(FWwiseArchive Ar)
{
    public iZTrashBoxModelerNonRTPCParams NonRTPC = Ar.Read<iZTrashBoxModelerNonRTPCParams>();
    public iZTrashBoxModelerRTPCParams RTPC = Ar.Read<iZTrashBoxModelerRTPCParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZTrashBoxModelerNonRTPCParams
{
    public uint uBoxModel;
    public uint uMicType;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZTrashBoxModelerRTPCParams
{
    public float fInputGain;
    public float fOutputGain;
    public float fMix;
    public float fTrim;
    public float fLength;
}
