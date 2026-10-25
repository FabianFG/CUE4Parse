using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Lua;
using CUE4Parse.UE4.Pak.Objects;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;

public static class RocoKingdomWorldEncryption
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FConfigurableCryptoInfoHeader
    {
        public ulong Magic;
        public uint StrategyIndex;
        public int DecryptedSize;
    }

    public static byte[] DecryptFile(byte[] data, FPakEntry entry, FAesKey? aesKey)
    {
        var headerSize = Unsafe.SizeOf<FConfigurableCryptoInfoHeader>();
        if (data.Length >= headerSize && BinaryPrimitives.ReadUInt64LittleEndian(data) == 0x7C3F9A215E8B4D26)
        {
            var cryptoHeader = MemoryMarshal.Read<FConfigurableCryptoInfoHeader>(data);
            data = FConfigurableCrypto.Decrypt(data[headerSize..], (byte) cryptoHeader.StrategyIndex, aesKey, entry.Name)[..cryptoHeader.DecryptedSize];
        }

        return entry.Extension == "luac" ? NRCLua.DecryptLuaBytecode(entry.Path, data) : data;
    }
}
