using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIoFileIndexEntry
    {
        public readonly uint Name;
        public readonly uint NextFileEntry;
        public readonly uint UserData;
    }
}