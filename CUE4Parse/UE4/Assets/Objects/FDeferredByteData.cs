using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.IO.Objects;

namespace CUE4Parse.UE4.Wwise;

public enum EDeferredDataSource
{
    Array,
    GameFile,
    BulkData,
}

public sealed class FDeferredByteData
{
    public readonly EDeferredDataSource Type;
    private FByteBulkDataHeader? _header;
    private GameFile? _file;
    private FByteBulkData? _bulkData;
    private byte[]? _data;

    public FDeferredByteData(byte[] data)
    {
        Type = EDeferredDataSource.Array;
        _data = data;
    }

    public FDeferredByteData(GameFile file, long offset, int size)
    {
        Type = EDeferredDataSource.GameFile;
        _file = file;
        _header = new FByteBulkDataHeader(EBulkDataFlags.BULKDATA_None, size, (uint) size, offset, FBulkDataCookedIndex.Default);
    }

    public FDeferredByteData(FAssetArchive ar, FByteBulkDataHeader header, long offset, int size)
    {
        Type = EDeferredDataSource.BulkData;
        _header = new FByteBulkDataHeader(header.BulkDataFlags, size, (uint) size, header.OffsetInFile + offset, header.CookedIndex);
        _bulkData = new FByteBulkData(ar, _header);
    }

    public bool IsValid => Type switch
    {
        EDeferredDataSource.Array => _data is { Length: > 0 },
        EDeferredDataSource.GameFile => _file is not null && _header is not null,
        EDeferredDataSource.BulkData => _bulkData is not null,
        _ => false
    };

    public byte[] GetData()
    {
        byte[]? data = Type switch
        {
            EDeferredDataSource.Array => _data,
            EDeferredDataSource.GameFile when _file != null && _file.TryRead(out var outdata, _header) => outdata,
            EDeferredDataSource.BulkData => _bulkData?.Data,
            _ => null,
        };
        return data ?? [];
    }

    public long LoadedSize() => _data is null ? 0 : _data.Length;
}
