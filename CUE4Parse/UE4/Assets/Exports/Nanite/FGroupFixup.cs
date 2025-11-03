using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

[StructLayout(LayoutKind.Sequential)]
public struct FGroupFixup
{
    public FPageRangeKey PageDependencies;
    public uint Flags;
    public ushort FirstPartFixup;
    public ushort NumPartFixup;
    public ushort FirstParentFixup;
    public ushort NumParentFixups;
}