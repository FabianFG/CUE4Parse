using System.Buffers.Binary;
using System.Reflection;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Pak.Objects;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace CUE4Parse.GameTypes.ChasingKaleidoRIDER.Encryption;

// this is not ideal, but but is faster than manual reimplementation of the customized ChaCha20 algorithm
public static class CKREncryption
{
#pragma warning disable IDE1006
    private static readonly FieldInfo? engineState = typeof(Salsa20Engine).GetField(nameof(engineState), BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? keyStream = typeof(Salsa20Engine).GetField(nameof(keyStream), BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? index = typeof(Salsa20Engine).GetField(nameof(index), BindingFlags.NonPublic | BindingFlags.Instance);
#pragma warning restore IDE1006
    private const int BlockSize = 64;
    public static byte[] CKRDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if ((count & 0xF) != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var (encryptionBaseOffset, position) = customData switch
        {
            FPakCustomEncryptionData data => (data.Entry.Offset + data.Entry.StructSize, data.Position),
            long indexOffset => (indexOffset, indexOffset),
            null when reader is PakFileReader pak => (pak.Info.IndexOffset, pak.Info.IndexOffset),
            _ => throw new ArgumentException("Unsupported CKR custom encryption data", nameof(customData))
        };

        var byteOffset = position - encryptionBaseOffset;
        var counter = byteOffset / BlockSize;
        var blockOffset = (int) (byteOffset % BlockSize);

        var chacha = new ChaChaEngine(12);
        Span<byte> iv = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(iv, encryptionBaseOffset);
        ParametersWithIV parameters = new ParametersWithIV(new KeyParameter(reader.AesKey.Key), iv);
        chacha.Init(false, parameters);

        if (engineState == null)
            throw new NullReferenceException("Salsa20Engine engineState is null");

        uint[] state = (uint[]) engineState.GetValue(chacha)!;
        state[12] = (uint) counter;
        state[13] = (uint) (counter >> 32);
        
        var output = new byte[count];
        if (blockOffset != 0 )
        {
            if (keyStream is null || index is null)
                throw new NullReferenceException("Salsa20Engine keyStream or index is null");

            byte[] key = (byte[]) keyStream.GetValue(chacha)!;
            int indexValue = (int) index.GetValue(chacha)!;
            var outputOffset = 0;
            var blockLength = Math.Min((BlockSize - blockOffset), count - outputOffset);
            chacha.ProcessBytes(bytes, beginOffset, blockLength, output, 0);
            for (var i = 0; i < blockLength; i++)
                output[outputOffset + i] = (byte)(bytes[beginOffset + outputOffset + i] ^ key[blockOffset + i]);
            outputOffset += blockLength;

            index.SetValue(chacha, 0);
            chacha.ProcessBytes(bytes, beginOffset + outputOffset, count - outputOffset, output, outputOffset);
        }
        else
        {
            chacha.ProcessBytes(bytes, beginOffset, count, output, 0);
        }

        return output;
    }
}
