using System.IO;
using CUE4Parse.UE4.FMod.Enums;
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
        var latestVersion = (int) EFModVersion.NEWEST_SUPPORTED_FILEVERSION;
        if (FileVersion > latestVersion)
        {
            Log.Warning($"FMod version 0x{FileVersion:X} is not supported, latest supported version is 0x{latestVersion:X}");
        }
        CompatVersion = Ar.ReadInt32();
    }
}
