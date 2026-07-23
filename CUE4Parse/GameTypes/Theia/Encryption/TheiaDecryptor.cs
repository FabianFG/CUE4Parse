using System.Collections.Concurrent;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using Blake3;
using CommunityToolkit.HighPerformance;
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
    public const int PageSize = 0x10000;
    public const int BlockSize = 64;

    private const int ArchiveSizeOffset = 80;
    private const int HeaderSize = 512;
    private const int KeyOffset = 32;
    private const int RecordSize = 64;
    private static ReadOnlySpan<byte> MetaMagic => "metadat0"u8;

    private readonly uint[] _masterKey;
    private TheiaPageKey[] _pageKeys = [];
    private readonly ConcurrentDictionary<int, TheiaState> _pageStates = [];

    public TheiaDecryptor(byte[] meta, long fileSize)
    {
        if (meta.Length < MetaMagic.Length || !meta.AsSpan(0, MetaMagic.Length).SequenceEqual(MetaMagic))
            throw new InvalidDataException("Invalid Theia metadata: missing metadat0 magic");

        using var Ar = new GenericBufferReader(meta);
        Ar.Position = ArchiveSizeOffset;
        if (Ar.Read<long>() != fileSize)
            throw new InvalidDataException("Invalid Theia metadata: file size mismatch");

        Ar.Position = ArchiveSizeOffset + 16;
        // For Arc Raiders this key is constant
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

            var state = _pageStates.GetOrAdd(page, static (page, self) => TheiaSchedule.InitPageState(self._pageKeys[page], self._masterKey), this);
            DecryptPageRange(data, destinationOffset, pageOffset, overlapLength, state);
        }
    }

    private void DecryptPageRange(Span<byte> data, int destinationOffset, int pageOffset, int length, TheiaState state)
    {
        var firstBlock = pageOffset / BlockSize;
        var lastBlock = (pageOffset + length - 1) / BlockSize;
        Span<byte> keystream = stackalloc byte[BlockSize];
        for (var block = firstBlock; block <= lastBlock; block++)
        {
            TheiaSchedule.KeystreamBlock(state, (ulong) block, keystream);
            var start = Math.Max(pageOffset, block * BlockSize);
            var end = Math.Min(pageOffset + length, (block + 1) * BlockSize);
            var keystreamOffset = start - block * BlockSize;
            var outputOffset = destinationOffset + start - pageOffset;
            var span = data.Slice(outputOffset, end - start);
            TensorPrimitives.Subtract(span, keystream.Slice(keystreamOffset, end - start), span);
        }
    }
}
