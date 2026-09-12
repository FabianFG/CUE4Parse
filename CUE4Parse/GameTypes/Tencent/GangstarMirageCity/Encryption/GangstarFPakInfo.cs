using CUE4Parse.GameTypes.Tencent.GangstarMirageCity.Encryption;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak.Objects;

public partial class FPakInfo
{
    private static unsafe void DecryptGangstarFPakInfo(long maxOffset, byte* buffer)
        => TensorUtils.Xor(new Span<byte>(buffer, (int) maxOffset), GangstarMirageCityAes._xorKey);
}
