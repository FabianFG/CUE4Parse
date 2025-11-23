using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct FIoMetaFileIndexEntry
{
    public uint Name;
    public uint ContainerName;
    public uint DirectoryEntry;
    public uint NextFileEntry;
}
