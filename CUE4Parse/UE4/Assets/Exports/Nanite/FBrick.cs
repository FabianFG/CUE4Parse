using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public struct FBrick
{
    private const int BrickSize = 20;

    TIntVector2<uint> ReverseBrickBits;
    FIntVector StartPos;
    TIntVector3<uint> BrickMax;
    uint VertOffset;
    uint BoneIndex;

    // DecodeBrick
    public FBrick(FArchive Ar, long pageBaseAddress, uint brickDataOffset, int brickIndex)
    {
        Ar.Position = pageBaseAddress + brickDataOffset + BrickSize * brickIndex;
        ReverseBrickBits = Ar.Read<TIntVector2<uint>>();
        var zw = Ar.Read<ulong>();
        var z = (uint)zw;
        var w = (uint)(zw >> 32);
        BrickMax = new (NaniteUtils.GetBits(z, 2, 0) + 1, NaniteUtils.GetBits(z, 2, 2) + 1, NaniteUtils.GetBits(z, 2, 4) + 1);
        StartPos = new((int)NaniteUtils.GetBits(z, 19, 6), (int)NaniteUtils.GetBits((uint) (zw >> 25), 19, 0), (int)NaniteUtils.GetBits(w, 19, 12));
        var offset_index = Ar.Read<uint>();
        VertOffset = offset_index & 0xFFFF;
        BoneIndex = offset_index >> 16;
    }
}
