using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public readonly struct FMutableStreamableBlock(FArchive Ar)
{
    public readonly uint FileId = Ar.Read<uint>();
    public readonly uint Flags = Ar.Game >= Versions.EGame.GAME_UE5_6 ? Ar.Read<ushort>() : Ar.Read<uint>();
    public readonly ulong Offset = Ar.Read<ulong>();
}
