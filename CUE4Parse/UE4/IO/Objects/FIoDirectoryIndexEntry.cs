using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIoDirectoryIndexEntry
    {
        public readonly uint Name;
        public readonly uint FirstChildEntry;
        public readonly uint NextSiblingEntry;
        public readonly uint FirstFileEntry;
    }
}