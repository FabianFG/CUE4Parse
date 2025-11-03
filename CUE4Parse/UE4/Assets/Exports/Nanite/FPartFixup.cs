using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

[StructLayout(LayoutKind.Sequential)]
public struct FPartFixup
{
    public ushort PageIndex;
    public byte StartClusterIndex;
    public byte LeafCounter;
    public uint FirstHierarchyLocation;
    public uint NumHierarchyLocations;
}