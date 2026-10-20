using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects;

public partial class FIoStoreTocHeader
{
    private static bool HasArcheAgeChroniclesTocMagic(FArchive archive, ReadOnlySpan<byte> magic) =>
        archive.Game == EGame.GAME_ArcheAgeChronicles && magic.SequenceEqual("X5=IoStore=Toc=!"u8);

    // X5 adds two reserved uint32 fields between the IoStore version and header size.
    private static void ReadArcheAgeChroniclesTocHeaderExtension(FArchive archive)
    {
        _ = archive.Read<uint>();
        _ = archive.Read<uint>();
    }
}
