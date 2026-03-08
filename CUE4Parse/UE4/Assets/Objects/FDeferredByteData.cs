using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.IO.Objects;

namespace CUE4Parse.UE4.Wwise;

public abstract class FDeferredByteData
{
    public abstract bool IsValid { get; }

    public abstract byte[] GetData();

    public virtual long LoadedSize => 0;
}

public sealed class FArrayDeferredByteData(byte[] data) : FDeferredByteData
{
    private readonly byte[] _data = data;

    public override bool IsValid => _data is {Length: > 0 };

    public override byte[] GetData() => _data;

    public override long LoadedSize => _data.Length;
}

public sealed class FGameFileDeferredByteData : FDeferredByteData
{
    private readonly FByteBulkDataHeader? _header;
    public readonly GameFile File;

    public FGameFileDeferredByteData(GameFile file, long offset = 0, int size = -1)
    {
        File = file;
        _header = offset == 0 && size == -1 ? null : new FByteBulkDataHeader(EBulkDataFlags.BULKDATA_None, size, (uint) size, offset, FBulkDataCookedIndex.Default);
    }

    public override bool IsValid => File is not null;

    public override byte[] GetData() => File.TryRead(out var data, _header) ? data : [];
}

public sealed class FBulkDataDeferredByteData : FDeferredByteData
{
    public readonly FByteBulkData BulkData;

    public FBulkDataDeferredByteData(FAssetArchive Ar, FByteBulkDataHeader header, long offset, int size)
    {
        var bulkDataHeader = new FByteBulkDataHeader(header.BulkDataFlags, size, (uint) size, header.OffsetInFile + offset, header.CookedIndex);
        BulkData = new FByteBulkData(Ar, bulkDataHeader);
    }

    public override bool IsValid => BulkData is not null;

    public override byte[] GetData() => BulkData.Data ?? [];
}
