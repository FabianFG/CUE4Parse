using System.IO;
using Serilog;

namespace CUE4Parse.UE4.FMod.Metadata;

public readonly struct FFormatInfo
{
    public readonly int FileVersion;
    public readonly int CompatVersion;

    public FFormatInfo(BinaryReader Ar)
    {
        FileVersion = Ar.ReadInt32();
#if DEBUG
        Log.Debug($"FMod soundbank version: 0x{FileVersion:X}");
#endif
        CompatVersion = Ar.ReadInt32();
    }
}
