using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

[StructLayout(LayoutKind.Sequential)]
public struct FParentFixup
{
    public ushort PageIndex;
    public ushort PartFixupPageIndex;
    public ushort PartFixupIndex;
    public ushort NumClusterIndices;
    public ushort FirstClusterIndex;
}