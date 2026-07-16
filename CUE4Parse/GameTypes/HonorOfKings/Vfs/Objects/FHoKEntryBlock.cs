using System.Runtime.InteropServices;

namespace CUE4Parse.GameTypes.HonorOfKings.Vfs.Objects;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FHoKHeader
{
    public ulong Magic;
    public int Size;
    public int FullSize;
    public FHoKEntriesOffsetSize Unknown0;
    public FHoKEntriesOffsetSize Unknown1; // always -1
    public FHoKEntriesOffsetSize Index;
    public FHoKEntriesOffsetSize IndexData;
    public FHoKEntriesOffsetSize Entries1;
    public FHoKEntriesOffsetSize Entries2;
    public FHoKEntriesOffsetSize Entries3;
    public FHoKEntriesOffsetSize Unknown2; // always -1
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FHoKEntriesTable
{
    public int Offset1;
    public int Offset2;
    public int Offset3;
    public int Unknown1;
    public int Unknown2;
    public int Unknown3;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FHoKEntriesOffsetSize
{
    public int Offset;
    public int Size;
}

[StructLayout(LayoutKind.Sequential)]
public struct FHoKEntryBlock
{
    public int Offset;
    public int Unknown;
    public int NextOffset;
    public ushort EntryCount;
    public ushort Type;
}
