using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.IO.Objects;

namespace CUE4Parse.UE4.Assets.Objects;

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

public sealed class FRangeDeferredByteData(FDeferredByteData source, int offset, int size) : FDeferredByteData
{
    public override bool IsValid => source.IsValid && size > 0;
    public override byte[] GetData()
    {
        var data = source.GetData();
        return offset == 0 && data.Length == size ? data : data.AsSpan(offset, size).ToArray();
    }
}

public sealed class FCombinedBulkDeferredByteData(FByteBulkData first, FByteBulkData second) : FDeferredByteData
{
    public override bool IsValid => first.GetDataSize() + second.GetDataSize() > 0;
    public override byte[] GetData()
    {
        var firstData = first.ReadDataOnce() ?? [];
        var secondData = second.ReadDataOnce() ?? [];
        return [.. firstData, .. secondData];
    }
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

    public FBulkDataDeferredByteData(FAssetArchive Ar, FByteBulkData bulkData, long offset, int size)
    {
        BulkData = new FByteBulkData(Ar, bulkData, offset, size);
    }

    public override bool IsValid => BulkData is not null;

    public override byte[] GetData() => BulkData.ReadDataOnce() ?? [];
}
