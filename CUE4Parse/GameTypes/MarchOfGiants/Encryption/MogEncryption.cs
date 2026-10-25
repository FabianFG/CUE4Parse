using System.Buffers.Binary;
using System.Resources;
using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.GameTypes.MarchOfGiants.Encryption;

// Reversed by Spiritovod
public static class MogEncryption
{
    private const int KeyEntrySize = 33;
    private static readonly byte[] _keyPool = LoadKeyPool();
    internal static int KeyCount => _keyPool.Length / KeyEntrySize;

    private static byte[] LoadKeyPool()
    {
        using var stream = typeof(MogEncryption).Assembly.GetManifestResourceStream("CUE4Parse.Resources.MogKeyPool.bin") ?? throw new MissingManifestResourceException("Couldn't find MogKeyPool.bin in Embedded Resources");
        var pool = new byte[stream.Length];
        stream.ReadExactly(pool);

        return pool;
    }

    internal static FAesKey GetHeaderKey(int index)
    {
        var key = _keyPool.AsSpan(index * KeyEntrySize, 32).ToArray();
        BinaryPrimitives.WriteUInt64LittleEndian(key, 0x5424B1A93FB9678A);
        return new FAesKey(key);
    }

    public static FAesKey GetKey(ReadOnlySpan<byte> seed)
    {
        if (seed.Length < 16)
            throw new ArgumentException("MOG key derivation requires a 16-byte seed", nameof(seed));

        var seedA = BinaryPrimitives.ReadUInt64LittleEndian(seed);
        var seedB = BinaryPrimitives.ReadUInt64LittleEndian(seed[8..]);
        var mixed = unchecked(0x9E3779B97F4A7C15 * seedB);
        var prefix = seedA ^ mixed;

        var index = 0;
        for (var bit = 0; bit < 8; bit++)
            index |= (int) ((prefix >> (bit * 9)) & 1) << bit;

        // There's this flag too in the pool and if it's not 1 then empty key
        if (_keyPool[index * KeyEntrySize + 32] != 1)
            return new FAesKey(new byte[32]);

        var key = _keyPool.AsSpan(index * KeyEntrySize, 32).ToArray();
        if (mixed != seedA)
            BinaryPrimitives.WriteUInt64LittleEndian(key, prefix);

        return new FAesKey(key);
    }
}
