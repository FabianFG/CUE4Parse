using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FOnDemandTocVersion
{
    public readonly EOnDemandTocMajorVersion Major;
    public readonly EOnDemandTocMinorVersion Minor;

    public bool IsValid() => Major is > EOnDemandTocMajorVersion.Invalid and < EOnDemandTocMajorVersion.LatestPlusOne &&
                             Minor is > EOnDemandTocMinorVersion.Invalid and < EOnDemandTocMinorVersion.LatestPlusOne;

    public override string ToString() => $"Major: {Major} Minor: {Minor} IsValid: {IsValid()}";
}

public enum EOnDemandTocMajorVersion : ushort
{
    Invalid			= 0,
    One				= 1,

    LatestPlusOne,
    Latest			= (LatestPlusOne - 1)
}

public enum EOnDemandTocMinorVersion : ushort
{
    Invalid			= 0,
    MemoryMapped	= 1,
    Partitions		= 2,

    LatestPlusOne,
    Latest			= (LatestPlusOne - 1)
}