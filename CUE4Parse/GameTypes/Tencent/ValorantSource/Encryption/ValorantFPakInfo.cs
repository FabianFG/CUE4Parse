using CUE4Parse.GameTypes.Tencent.ValorantSource.Encryption.Aes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak.Objects;

public partial class FPakInfo
{
    private static unsafe void DecryptValorantSourcePakInfo(FArchive Ar, long maxOffset, byte* buffer)
    {
        TensorUtils.Xor(new Span<byte>(buffer, (int) maxOffset), ValorantSourceAes.ZucXorTableBytes);
    }
}
