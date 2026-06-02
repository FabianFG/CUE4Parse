using System;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Wwise.Plugins.Mindseye;

public class AudioDataPassbackFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public bool[] unknown1 = Ar.ReadArray(4, () => Ar.Read<byte>() != 0);
}

public class BarbDelayFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public float unknown1 = Ar.Read<float>();
    public float unknown2 = Ar.Read<float>() * 0.01f;
    public float unknown3 = Ar.Read<float>() * 0.01f;
    public float unknown4 = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
    public byte unknown5 = Ar.Read<byte>();
    public byte unknown6 = Ar.Read<byte>();
}

public class BarbRecorderFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public float unknown1 = Ar.Read<float>();
}

public class DrunkPMSourceParams(FWwiseArchive Ar) : IAkPluginParam
{
    public TPair<int> unknown1 = Ar.Read<TPair<int>>();
    public TPair<float> unknown2 = Ar.Read<TPair<float>>();
    public TPair<float> unknown3 = Ar.Read<TPair<float>>();
    public int unknown4 = Ar.Read<int>();
}
