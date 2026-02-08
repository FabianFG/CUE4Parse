using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashMultibandDistortionFXParams(FArchive Ar) : IAkPluginParam
{
    public TrashMultibandDistortionFXParams Params = Ar.Read<TrashMultibandDistortionFXParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct TrashMultibandDistortionFXParams
{
    public float Distortion1Band1InputGain;
    public float Distortion1Band1Overdrive;
    public float Distortion1Band1Trash;
    public float Distortion1Band1Mix;
    public float Distortion1Band1OutputGain;
    public uint Distortion1Band1Type;

    public float Distortion2Band1InputGain;
    public float Distortion2Band1Overdrive;
    public float Distortion2Band1Trash;
    public float Distortion2Band1Mix;
    public float Distortion2Band1OutputGain;
    public uint Distortion2Band1Type;

    public float Distortion1Band2InputGain;
    public float Distortion1Band2Overdrive;
    public float Distortion1Band2Trash;
    public float Distortion1Band2Mix;
    public float Distortion1Band2OutputGain;
    public uint Distortion1Band2Type;

    public float Distortion2Band2InputGain;
    public float Distortion2Band2Overdrive;
    public float Distortion2Band2Trash;
    public float Distortion2Band2Mix;
    public float Distortion2Band2OutputGain;
    public uint Distortion2Band2Type;

    public float Distortion1Band3InputGain;
    public float Distortion1Band3Overdrive;
    public float Distortion1Band3Trash;
    public float Distortion1Band3Mix;
    public float Distortion1Band3OutputGain;
    public uint Distortion1Band3Type;

    public float Distortion2Band3InputGain;
    public float Distortion2Band3Overdrive;
    public float Distortion2Band3Trash;
    public float Distortion2Band3Mix;
    public float Distortion2Band3OutputGain;
    public uint Distortion2Band3Type;

    public float MultibandCrossover1;
    public float MultibandCrossover2;
}
