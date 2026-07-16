using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;

namespace CUE4Parse.GameTypes.LotF.Assets.Objects.Mutables;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 25)]
public struct FLotFImageHeader
{
    public int idk0;
    public int idk1;
    public int idk2;
    public int idk3;
    public EImageFormat Format;
    private ushort _padding0;
    private byte _padding1;
    public byte Mips;
    public ushort SizeX;
    public ushort SizeY;
}
