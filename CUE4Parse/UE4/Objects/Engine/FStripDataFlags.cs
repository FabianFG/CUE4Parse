using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FStripDataFlags
    {
        public readonly byte GlobalStripFlags;
        public readonly byte ClassStripFlags;

        public FStripDataFlags(FAssetArchive Ar, int minVersion = 130)
        {
            if ((int)Ar.Ver >= minVersion)
            {
                GlobalStripFlags = Ar.Read<byte>();
                ClassStripFlags = Ar.Read<byte>();
            }
            else
            {
                GlobalStripFlags = ClassStripFlags = 0;
            }
        }

        public bool IsEditorDataStripped() => (GlobalStripFlags & 1) != 0;
        public bool IsDataStrippedForServer() => (GlobalStripFlags & 2) != 0;
        public bool IsClassDataStripped(byte flag) => (ClassStripFlags & flag) != 0;
    }
}