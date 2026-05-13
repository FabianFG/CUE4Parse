using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

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

    public FImageLODRange(FMutableArchive Ar)
    {
        FirstIndex = Ar.Read<int>();
        if (Ar.Game < EGame.GAME_UE5_3)
        {
            LODCount = (byte)Ar.Read<int>();
        }
        ImageSizeX = Ar.Read<ushort>();
        ImageSizeY = Ar.Read<ushort>();
        
        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            LODCount = Ar.Read<byte>();
            NumLODsInTail = Ar.Read<byte>();
            Flags = Ar.Read<byte>();
            ImageFormat = Ar.Read<EImageFormat>();
        }
        else if (Ar.Game >= EGame.GAME_UE5_3)
        {
            Ar.Position += 2;
            LODCount = Ar.Read<byte>();
            ImageFormat = Ar.Read<EImageFormat>();
        }
    }
}
