using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Image;

public class FImageLODRange
{
    public int FirstIndex;
    public ushort ImageSizeX;
    public ushort ImageSizeY;
    public ushort Padding;
    public byte LODCount;
    public EImageFormat ImageFormat;

    public FImageLODRange(FArchive Ar)
    {
        FirstIndex = Ar.Read<int>();
        ImageSizeX = Ar.Read<ushort>();
        ImageSizeY = Ar.Read<ushort>();
        Padding = Ar.Read<ushort>();
        LODCount = Ar.Read<byte>();
        ImageFormat = Ar.Read<EImageFormat>();
    }
}
