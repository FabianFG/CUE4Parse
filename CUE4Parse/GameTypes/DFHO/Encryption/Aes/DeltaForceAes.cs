using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.DFHO.Encryption.Aes;

public static class DeltaForceAes
{
    public static byte[] DeltaForceDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");
        if ((reader as PakFileReader)?.Info?.CustomEncryptionData is not { Length: 2 } xorData)
            throw new NullReferenceException("reader.Info");

        var output = AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);

        if (isIndex && xorData[1] == 0)
        {
            xorData[0] = output[0];
            xorData[1] = 1;
        }

        TensorUtils.Xor(output, xorData[0]);

        return output;
    }
}
