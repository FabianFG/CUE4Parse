using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FExportBundleHeader(FArchive Ar)
{
    public readonly ulong SerialOffset = Ar.Game >= EGame.GAME_UE5_0 ? Ar.Read<ulong>() : ulong.MaxValue;
    public readonly uint FirstEntryIndex = Ar.Read<uint>();
    public readonly uint EntryCount = Ar.Read<uint>();
}
