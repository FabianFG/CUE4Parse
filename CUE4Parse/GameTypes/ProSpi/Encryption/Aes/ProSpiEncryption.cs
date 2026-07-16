using System.Collections.Concurrent;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.VirtualFileSystem;
using Serilog;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ProSpiEncryption));
    private static readonly ConcurrentDictionary<ulong, byte> _missingDescriptorLookupKeys = new();

    public const int EncryptionDataTrailerSize = 0x18;
    public const string ExpectedExeHash = "2F6189590F1896E48E752FD08D436BD276A02086FCB38B75EF5042DB7FCDD3DC"; // DLL was mapped ONLY with this executable

    private static byte[] _aesKey = [];
    private static string _exePath = string.Empty;

    public static byte[] ProSpiDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        _aesKey = reader.AesKey.Key;
        if (string.IsNullOrEmpty(_exePath))
        {
            _exePath = GetExePathFromReader(reader);
        }

        if (string.IsNullOrEmpty(_exePath))
            throw new InvalidOperationException("Could not determine ProSpi module path, cannot decrypt.");

        var input = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, input, 0, count);

        if (TryPortedDecrypt(input, out var staticOutput))
            return staticOutput;

        // In short, there were too many algorithms to cover manually
        // so instead mapped descriptors to algorithm RVAs and let module handle the decryption
        // (it's static but original module is needed)
        if (TryDllDecrypt(input, out var liftedOutput))
            return liftedOutput;

        throw new ParserException($"ProSpi decryption failed for {count} bytes at offset {beginOffset}.");
    }

    private static bool TryPortedDecrypt(byte[] input, out byte[] output)
    {
        output = [];
        if (input.Length < EncryptionDataTrailerSize)
            return false;

        var trailer = input.AsSpan(input.Length - EncryptionDataTrailerSize, EncryptionDataTrailerSize);
        if (!TryResolveCipherSpec(trailer, out var spec))
            return false;

        output = new byte[input.Length];
        Buffer.BlockCopy(input, 0, output, 0, input.Length);
        Decrypt(output.AsSpan(0, input.Length - EncryptionDataTrailerSize), trailer, spec);
        return true;
    }
}
