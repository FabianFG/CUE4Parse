using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4;

namespace CUE4Parse.GameTypes.Brickadia.Objects;

public struct FBrickStudGroup(FAssetArchive Ar) : IUStruct
{
    public FIntVector Offset = Ar.Read<FIntVector>();
    public byte NumX = Ar.Read<byte>();
    public byte NumY = Ar.Read<byte>();
    public byte Direction = Ar.Read<byte>();
    public byte Type = Ar.Read<byte>();
}
