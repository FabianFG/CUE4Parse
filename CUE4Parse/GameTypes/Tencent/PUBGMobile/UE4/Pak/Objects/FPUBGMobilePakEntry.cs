using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.UE4.Pak.Objects;

public sealed class FPUBGMobilePakEntry(PakFileReader reader, FArchive Ar) : FPakEntry(reader, "", Ar, GAME_PUBGMobile)
{
    public required int EncryptionMethod { get; init; }
    public required uint EncryptionKeyId { get; init; }
}
