using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FStripDataFlags
    {
        public readonly byte GlobalStripFlags;
        public readonly byte ClassStripFlags;

        public bool IsEditorDataStripped() => (GlobalStripFlags & 1) != 0;
        public bool IsDataStrippedForServer() => (GlobalStripFlags & 2) != 0;
        public bool IsClassDataStripped(byte flag) => (ClassStripFlags & flag) != 0;
    }
}