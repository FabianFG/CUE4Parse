using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public readonly struct FMutableStreamableBlock(FArchive Ar)
{
    public readonly uint FileId = Ar.Read<uint>();
    public readonly uint Flags = Ar.Game >= EGame.GAME_UE5_6 ? Ar.Read<ushort>() : Ar.Read<uint>();
    public readonly ulong Offset = Ar.Read<ulong>();
}
