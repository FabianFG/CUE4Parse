using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashDistortionFXParams(FArchive Ar) : IAkPluginParam
{
    public iZTrashDistortionFXParams Params = new iZTrashDistortionFXParams(Ar);
}

public struct iZTrashDistortionFXParams(FArchive Ar)
{
    public iZTrashDistortionRTPCParams RTPC = Ar.Read<iZTrashDistortionRTPCParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZTrashDistortionRTPCParams
{
    public float Distortion1InputGain;
    public float Distortion1Overdrive;
    public float Distortion1Trash;
    public float Distortion1Mix;
    public float Distortion1OutputGain;
    public uint Distortion1Type;
    public float Distortion2InputGain;
    public float Distortion2Overdrive;
    public float Distortion2Trash;
    public float Distortion2Mix;
    public float Distortion2OutputGain;
    public uint Distortion2Type;
}

