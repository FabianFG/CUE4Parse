using CUE4Parse.GameTypes.Tencent.ValorantSource.Encryption.Aes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Pak.Objects;

public partial class FPakInfo
{
    private static unsafe void DecryptValorantSourcePakInfo(FArchive Ar, long maxOffset, byte* buffer)
    {
        var zuc128XorTable = ValorantSourceAes.ZucXorTableBytes;
        for (long i = 0; i < maxOffset; i++)
        {
            buffer[i] ^= zuc128XorTable[i % zuc128XorTable.Length];
        }
    }
}
