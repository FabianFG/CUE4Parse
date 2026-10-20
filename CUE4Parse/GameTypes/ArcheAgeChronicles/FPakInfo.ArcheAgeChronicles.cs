using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Pak.Objects;

public partial class FPakInfo
{
    public const uint PAK_FILE_MAGIC_ArcheAgeChronicles = 0x5835F00D;

    private const OffsetsToTry ArcheAgeChroniclesFooterOffset =
        (OffsetsToTry) ((int) OffsetsToTry.Size8a + sizeof(ulong));

    private static bool HasArcheAgeChroniclesPakMagic(FArchive archive, uint magic) =>
        archive.Game == EGame.GAME_ArcheAgeChronicles && magic == PAK_FILE_MAGIC_ArcheAgeChronicles;

    private static bool IsSupportedArcheAgeChroniclesPakMagic(uint magic) =>
        magic is PAK_FILE_MAGIC or PAK_FILE_MAGIC_ArcheAgeChronicles;

    private static OffsetsToTry[] GetArcheAgeChroniclesOffsetsToTry() =>
        [ArcheAgeChroniclesFooterOffset, .._offsetsToTry];

    // X5 adds one reserved uint64 field between the pak magic and version.
    private static void ReadArcheAgeChroniclesPakTrailerExtension(FArchive archive)
    {
        _ = archive.Read<ulong>();
    }
}
