namespace CUE4Parse.GameTypes.Theia.Encryption;

public static class TheiaDecryptor
{
    public const int PageSize = 0x10000;
    public const int BlockSize = 64;
    private static ReadOnlySpan<byte> MetaMagic => "metadat0"u8;

    public static void ValidateMeta(ReadOnlySpan<byte> meta, long fileSize)
    {
        if (meta.Length < MetaMagic.Length || !meta[..MetaMagic.Length].SequenceEqual(MetaMagic))
            throw new InvalidDataException("Invalid Theia metadata: missing metadat0 magic");

        var pages = GetPageCount(fileSize);
        var requiredSize = ((long) pages + 8) * 64;
        if (meta.Length < requiredSize)
            throw new InvalidDataException($"Theia metadata is too small: have {meta.Length}, need at least {requiredSize}");
    }

    public static int GetPageCount(long fileSize)
    {
        var pages = (fileSize + PageSize - 1) / PageSize;
        if (pages > int.MaxValue)
            throw new InvalidDataException($"File is too large for a Theia page table: {fileSize} bytes");

        return (int) pages;
    }

    public static void DecryptRangeInPlace(Span<byte> data, long fileOffset, ReadOnlySpan<byte> meta, Func<int, byte[]>? pageStateProvider = null)
    {
        if (data.IsEmpty)
            return;

        ArgumentOutOfRangeException.ThrowIfNegative(fileOffset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fileOffset, long.MaxValue - data.Length);

        var end = fileOffset + data.Length;
        var firstPage = checked((int) (fileOffset / PageSize));
        var lastPage = checked((int) ((end - 1) / PageSize));
        Span<byte> state = stackalloc byte[33];
        Span<byte> keystream = stackalloc byte[BlockSize];

        for (var page = firstPage; page <= lastPage; page++)
        {
            var pageStart = (long) page * PageSize;
            var overlapStart = Math.Max(fileOffset, pageStart);
            var overlapEnd = Math.Min(end, pageStart + PageSize);
            var overlapLength = (int) (overlapEnd - overlapStart);
            var destinationOffset = (int) (overlapStart - fileOffset);
            var pageOffset = (int) (overlapStart - pageStart);

            if (pageStateProvider is null)
            {
                TheiaSchedule.InitPageState(meta, page, state);
                DecryptPageRange(data, destinationOffset, pageOffset, overlapLength, state, keystream);
            }
            else
            {
                DecryptPageRange(data, destinationOffset, pageOffset, overlapLength, pageStateProvider(page), keystream);
            }
        }
    }

    private static void DecryptPageRange(Span<byte> data, int destinationOffset, int pageOffset, int length, ReadOnlySpan<byte> state, Span<byte> keystream)
    {
        var firstBlock = pageOffset / BlockSize;
        var lastBlock = (pageOffset + length - 1) / BlockSize;
        for (var block = firstBlock; block <= lastBlock; block++)
        {
            TheiaSchedule.KeystreamBlock(state, (ulong) block, keystream);
            var start = Math.Max(pageOffset, block * BlockSize);
            var end = Math.Min(pageOffset + length, (block + 1) * BlockSize);
            var keystreamOffset = start - block * BlockSize;
            var outputOffset = destinationOffset + start - pageOffset;
            unchecked
            {
                for (var i = 0; i < end - start; i++)
                    data[outputOffset + i] -= keystream[keystreamOffset + i];
            }
        }
    }
}
