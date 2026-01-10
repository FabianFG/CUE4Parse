using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;

[StructLayout(LayoutKind.Sequential)]
public struct FImageLODRange
{
    public int FirstIndex;
    public ushort ImageSizeX;
    public ushort ImageSizeY;
    public byte LODCount;
    public byte NumLODsInTail;
    public byte Flags;
    public EImageFormat ImageFormat;
}