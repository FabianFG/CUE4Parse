using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct FIoMetaDirectoryIndexEntry
{
    public uint Name;
    public uint ParentEntry;
    public uint FirstChildEntry;
    public uint NextSiblingEntry;
    public uint FirstFileEntry;
}
