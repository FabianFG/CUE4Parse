using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkRoomVerbFXParams(FArchive Ar) : IAkPluginParam
{
    public AkRoomVerbFXParams Params = new AkRoomVerbFXParams(Ar);
}

public struct AkRoomVerbFXParams(FArchive Ar)
{
    public AkRoomVerbRTPCParams RTPCParams = new AkRoomVerbRTPCParams(Ar);
    public AkRoomVerbInvariantParams InvariantParams = new AkRoomVerbInvariantParams(Ar);
    public AkRoomVerbAlgoTunings AlgoTunings = Ar.Read<AkRoomVerbAlgoTunings>();
}

public struct AkRoomVerbRTPCParams(FArchive Ar)
{
    public float DecayTime = Ar.Read<float>();
    public float HFDamping = Ar.Read<float>();
    public float Diffusion = Ar.Read<float>();
    public float StereoWidth = Ar.Read<float>();
    public float Filter1Gain = Ar.Read<float>();
    public float Filter1Freq = Ar.Read<float>();
    public float Filter1Q = Ar.Read<float>();
    public float Filter2Gain = Ar.Read<float>();
    public float Filter2Freq = Ar.Read<float>();
    public float Filter2Q = Ar.Read<float>();
    public float Filter3Gain = Ar.Read<float>();
    public float Filter3Freq = Ar.Read<float>();
    public float Filter3Q = Ar.Read<float>();
    public float FrontLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float RearLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float CenterLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float LFELevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float DryLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float ERLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float ReverbLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05 - 0.15);
}

public struct AkRoomVerbInvariantParams(FArchive Ar)
{
    public bool bEnableEarlyReflections = Ar.Read<byte>() != 0;
    public uint ERPattern = Ar.Read<uint>();
    public float ReverbDelay = Ar.Read<float>();
    public float RoomSize = Ar.Read<float>();
    public float ERFrontBackDelay = Ar.Read<float>();
    public float Density = Ar.Read<float>();
    public float RoomShape = Ar.Read<float>();
    public uint NumReverbUnits = Ar.Read<uint>();
    public bool bEnableToneControls = Ar.Read<byte>() != 0;
    public AkFilterInsertType Filter1Pos = Ar.Read<AkFilterInsertType>();
    public AkFilterCurveType Filter1Curve = Ar.Read<AkFilterCurveType>();
    public AkFilterInsertType Filter2Pos = Ar.Read<AkFilterInsertType>();
    public AkFilterCurveType Filter2Curve = Ar.Read<AkFilterCurveType>();
    public AkFilterInsertType Filter3Pos = Ar.Read<AkFilterInsertType>();
    public AkFilterCurveType Filter3Curve = Ar.Read<AkFilterCurveType>();
    public float InputCenterLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float InputLFELevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkRoomVerbAlgoTunings
{
    public float DensityDelayMin;
    public float DensityDelayMax;
    public float DensityDelayRdmPerc;
    public float RoomShapeMin;
    public float RoomShapeMax;
    public float DiffusionDelayScalePerc;
    public float DiffusionDelayMax;
    public float DiffusionDelayRdmPerc;
    public float DCFilterCutFreq;
    public float ReverbUnitInputDelay;
    public float ReverbUnitInputDelayRdmPerc;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkFilterInsertType : uint
{
    Off = 0x0,
    EROnly = 0x1,
    ReverbOnly = 0x2,
    ERAndReverb = 0x3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkFilterCurveType : uint
{
    LowShelf = 0x0,
    Peaking = 0x1,
    HighShelf = 0x2
}
