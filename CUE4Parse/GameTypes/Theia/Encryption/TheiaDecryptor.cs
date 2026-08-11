using System.Collections.Concurrent;
using System.Numerics.Tensors;
using System.Reflection;
using System.Runtime.CompilerServices;
using Blake3;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Versions;
using GenericReader;

namespace CUE4Parse.GameTypes.Theia.Encryption;

public struct TheiaState
{
    private const int SIZE = 33;

    public Bytes33 State;

    public TheiaState(ReadOnlySpan<byte> span)
    {
        State = default;
        span[..(SIZE - 1)].CopyTo(State);
        State[SIZE - 1] = 0x01;
    }

    [InlineArray(SIZE)]
    public struct Bytes33
    {
        private byte _elem0;
    }
}

public struct TheiaPageKey
{
    private const int SIZE = 32;

    public Bytes32 Key;

    public TheiaPageKey(GenericBufferReader Ar)
    {
        Key = default;
        Span<byte> span = Key;
        Ar.Read(span);
    }

    [InlineArray(SIZE)]
    public struct Bytes32
    {
        private byte _elem0;
    }
}

public class TheiaDecryptor
{
    // Reversed by xivy
    public static readonly uint[] ArcRaidersKey  = [0xBEDC8A41, 0xC2083AA2, 0x7A40B8E0, 0x07D37C60, 0x16B42333, 0x5F76DE9B, 0x53881F06, 0x107B149C];
    // Reversed by trumank
    public static readonly uint[] MarvelTokonKey = [0x99899BE5, 0x3958611C, 0x64BC2227, 0xDDD76F19, 0x356B9B15, 0x3841E7E8, 0xB52A7A85, 0xFA43C8E5];
    // Reversed by trumank
    public static readonly uint[] HighguardKey   = [0x63490CBF, 0x955C29C9, 0xB8AE5CB3, 0xF6C18D04, 0xA0C7C061, 0xA1F27398, 0xF52305B0, 0x7E12F14A];

    private delegate void CompressDelegate(ReadOnlySpan<uint> chainingValue, ReadOnlySpan<uint> blockWords, ulong counter, uint blockLength, uint flags, Span<uint> output);

    private static readonly CompressDelegate Compress = CreateCompress();

    private static CompressDelegate CreateCompress()
    {
        var type = typeof(Hasher).Assembly.GetType("Blake3.Blake3ManagedCore");
        if (type is null)
            throw new MissingMemberException("Blake3.Blake3ManagedCore");
        var method = type.GetMethod(nameof(Compress), BindingFlags.Static | BindingFlags.NonPublic)!;
        return method.CreateDelegate<CompressDelegate>();
    }
    
    private const int PageSize = 0x10000;
    private const int BlockSize = 64;
    private static ReadOnlySpan<byte> MetaMagic => "metadat0"u8;
    private const int ArchiveSizeOffset = 80;
    private const int HeaderSize = 512;
    private const int KeyOffset = 32;
    private const int RecordSize = 64;

    private readonly uint[] _constKey;
    private readonly uint[] _masterKey;
    private readonly TheiaPageKey[] _pageKeys = [];
    private readonly ConcurrentDictionary<int, TheiaState> _pageStates = [];

    public TheiaDecryptor(byte[] meta, long fileSize, EGame game)
    {
        if (meta.Length < MetaMagic.Length || !meta.AsSpan(0, MetaMagic.Length).SequenceEqual(MetaMagic))
            throw new InvalidDataException("Invalid Theia metadata: missing metadat0 magic");

        _constKey = game switch
        {
            GAME_ArcRaiders => ArcRaidersKey,
            GAME_MARVELTokonFightingSouls => MarvelTokonKey,
            GAME_Highguard => HighguardKey,
            _ => throw new ArgumentOutOfRangeException(nameof(game), game, $"TheiaDecryption is not supported for {game}"),
        };

        using var Ar = new GenericBufferReader(meta);
        Ar.Position = ArchiveSizeOffset;
        if (Ar.Read<long>() != fileSize)
            throw new InvalidDataException("Invalid Theia metadata: file size mismatch");

        Ar.Position = ArchiveSizeOffset + 16;
        var rawMasterKey = Ar.ReadArray<byte>(32);
        Span<byte> hashBlock = stackalloc byte[64];
        rawMasterKey.CopyTo(hashBlock);
        _masterKey = Hasher.Hash(hashBlock).AsSpan().Cast<byte, uint>().ToArray();

        var pages = (fileSize + PageSize - 1) / PageSize;
        if (pages > int.MaxValue)
            throw new InvalidDataException($"File is too large for a Theia page table: {fileSize} bytes");

        var requiredSize = pages * RecordSize + HeaderSize;

        if (Ar.Length < requiredSize)
            throw new InvalidDataException($"Theia metadata is too small: have {meta.Length}, need at least {requiredSize}");

        _pageKeys = new TheiaPageKey[pages];
        for (var i = 0; i < pages; i++)
        {
            Ar.Position = HeaderSize + i * RecordSize + KeyOffset;
            _pageKeys[i] = new TheiaPageKey(Ar);
        }
    }

    public void DecryptRangeInPlace(Span<byte> data, long fileOffset)
    {
        if (data.IsEmpty)
            return;

        ArgumentOutOfRangeException.ThrowIfNegative(fileOffset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fileOffset, long.MaxValue - data.Length);

        var end = fileOffset + data.Length;
        var firstPage = checked((int) (fileOffset / PageSize));
        var lastPage = checked((int) ((end - 1) / PageSize));
        if (lastPage >= _pageKeys.Length)
            throw new ArgumentOutOfRangeException(nameof(fileOffset), "Data exceeds the number of available pages.");

        for (var page = firstPage; page <= lastPage; page++)
        {
            var pageStart = (long) page * PageSize;
            var overlapStart = Math.Max(fileOffset, pageStart);
            var overlapEnd = Math.Min(end, pageStart + PageSize);
            var overlapLength = (int) (overlapEnd - overlapStart);
            var destinationOffset = (int) (overlapStart - fileOffset);
            var pageOffset = (int) (overlapStart - pageStart);

            var state = _pageStates.GetOrAdd(page, static (page, self) => InitPageState(self._pageKeys[page], self._masterKey, self._constKey), this);
            DecryptPageRange(data, destinationOffset, pageOffset, overlapLength, state);
        }
    }

    private void DecryptPageRange(Span<byte> data, int destinationOffset, int pageOffset, int length, TheiaState state)
    {
        var firstBlock = pageOffset / BlockSize;
        var lastBlock = (pageOffset + length - 1) / BlockSize;
        Span<byte> output = stackalloc byte[BlockSize];
        Span<uint> outputspan = output.Cast<byte, uint>();
        for (var block = firstBlock; block <= lastBlock; block++)
        {
            KeystreamBlock(state, (ulong) block, outputspan);
            var start = Math.Max(pageOffset, block * BlockSize);
            var end = Math.Min(pageOffset + length, (block + 1) * BlockSize);
            var keystreamOffset = start - block * BlockSize;
            var outputOffset = destinationOffset + start - pageOffset;
            var span = data.Slice(outputOffset, end - start);
            TensorPrimitives.Subtract(span, output.Slice(keystreamOffset, end - start), span);
        }
    }

    public static TheiaState InitPageState(TheiaPageKey pageKey, ReadOnlySpan<uint> masterKey, ReadOnlySpan<uint> constKey)
    {
        Span<uint> arg2 = stackalloc uint[16];
        masterKey.CopyTo(arg2);

        ReadOnlySpan<byte> pageKeySpan = pageKey.Key;
        pageKeySpan.Cast<byte, uint>().CopyTo(arg2[8..]);

        Span<byte> digest = stackalloc byte[64];
        Compress(constKey, arg2, 0, BlockSize, 0x11, digest.Cast<byte, uint>());
        return new TheiaState(digest);
    }

    public static void KeystreamBlock(TheiaState state, ulong block, Span<uint> output)
    {
        ReadOnlySpan<byte> state33 = state.State;
        var sb = (uint) state33[^1];
        Span<uint> zeros = stackalloc uint[16];
        Compress(state33[..^1].Cast<byte, uint>(), zeros, block, sb, sb ^ 0x1Bu, output);
    }
}
