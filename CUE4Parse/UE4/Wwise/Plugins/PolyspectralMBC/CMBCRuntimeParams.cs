using System;
using System.Text;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins.PolyspectralMBC;

public class CMBCRuntimeParams : IAkPluginParam
{
    public float PercentWet;
    public float InputGain;
    public float OutputGain;
    public int NumBands;
    public float[] Crossover;
    public MBCBandParams[] Bands;
    public bool BypassCenter;
    public bool BypassLFE;
    public bool BypassOther;
    public bool ClipOutput;
    public MBCSidechainMode SideChainMode;
    public string SidechainOutputName;

    public CMBCRuntimeParams(FArchive Ar, int size)
    {
        var start = Ar.Position; 
        var value = Ar.Read<float>();
        var mode = 0;
        if (value != -42.0f)
        {
            PercentWet = value;
        }
        else
        {
            mode = Ar.Read<int>();
            PercentWet = mode >= 1 ? Ar.Read<float>() : 0;
        }
        InputGain = Ar.Read<float>();
        OutputGain = Ar.Read<float>();
        NumBands = Ar.Read<int>();

        Crossover = Ar.ReadArray<float>(3);
        Bands = Ar.ReadArray<MBCBandParams>(4);

        if (mode >= 1)
        {
            BypassCenter = Ar.Read<int>() != 0;
            BypassLFE = Ar.Read<int>() != 0;
            BypassOther = Ar.Read<int>() != 0;
        }

        if (mode >= 2) ClipOutput = Ar.Read<int>() != 0; ;
        if (mode >= 3) SideChainMode = Ar.Read<MBCSidechainMode>();

        if (mode >= 4)
        {
            var end = Ar.Position;
            var strSize = Math.Clamp((int)(size - (end - start)), 0, 64);
            var temp = Ar.ReadArray<byte>(strSize);
            var ind = Array.IndexOf(temp, (byte)0)+1;
            SidechainOutputName = Encoding.UTF8.GetString(temp.AsSpan()[..ind]);
            Ar.Position = end + ind;
        }
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum MBCSidechainMode : int
{
    Off = 0x0,
    ReadSingle = 0x1,
    WriteSingle = 0x2,
    ReadMultiple = 0x3,
    WriteMultiple = 0x4
};

public struct MBCBandParams(FArchive Ar)
{
    public float Threshold = Ar.Read<float>();
    public float Ratio = Ar.Read<float>();
    public float Gain = Ar.Read<float>();
    public float Attack = Ar.Read<float>();
    public float Release = Ar.Read<float>();
    public bool Solo = Ar.Read<int>() != 0;
    public bool Bypass = Ar.Read<int>() != 0;
}
