using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FExportBundleHeader
    {
        public readonly ulong SerialOffset;
        public readonly uint FirstEntryIndex;
        public readonly uint EntryCount;

        public FExportBundleHeader(FArchive Ar)
        {
            SerialOffset = Ar.Game >= EGame.GAME_UE5_0 ? Ar.Read<ulong>() : ulong.MaxValue;
            FirstEntryIndex = Ar.Read<uint>();
            EntryCount = Ar.Read<uint>();
        }
    }
}