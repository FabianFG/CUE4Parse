using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FMutableStreamableBlock
{
    public readonly uint FileId;
    public readonly ushort Flags;
    public readonly ulong Offset;
}