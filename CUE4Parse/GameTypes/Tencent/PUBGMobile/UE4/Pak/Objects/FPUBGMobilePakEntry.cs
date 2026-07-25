using CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.SM4;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.UE4.Pak.Objects;

public sealed class FPUBGMobilePakEntry : FPakEntry
{
    public readonly EPUBGMobileEncryptionMethod EncryptionMethod;
    public readonly uint EncryptionKeyId;

    public FPUBGMobilePakEntry(PakFileReader reader, FArchive Ar) : base(reader, "", Ar, reader.Game)
    {
        if (reader.Game is GAME_PUBGLite)
        {
            EncryptionMethod = IsEncrypted ? EPUBGMobileEncryptionMethod.LiteSaltSM4 : 0;
            EncryptionKeyId = 0;
        }
        else
        {
            EncryptionMethod = (EPUBGMobileEncryptionMethod) Ar.Read<int>();
            EncryptionKeyId = Ar.Read<uint>();
        }
    }
}
