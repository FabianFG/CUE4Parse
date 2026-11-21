using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.Tests;

public class BulkDataCacheTests
{
    [Fact]
    public void ReloadableBulkDataCanReleaseAndReloadCachedBytes()
    {
        byte[] payload = [ 1, 2, 3, 4 ];
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((uint) EBulkDataFlags.BULKDATA_PayloadAtEndOfFile);
            writer.Write(payload.Length);
            writer.Write((uint) payload.Length);
            writer.Write((long) 20);
            writer.Write(payload);
        }

        var versions = new VersionContainer(EGame.GAME_UE4_27);
        var archive = new FAssetArchive(new FByteArchive("bulk-data-test", stream.ToArray(), versions), null);
        var bulkData = new FByteBulkData(archive);

        Assert.False(bulkData.TryReleaseCachedData());
        Assert.Equal(payload, bulkData.Data);
        Assert.True(bulkData.TryReleaseCachedData());
        Assert.Equal(payload, bulkData.Data);
    }

    [Fact]
    public void InMemoryBulkDataCannotReleaseItsOnlyCopy()
    {
        byte[] payload = [ 5, 6, 7, 8 ];
        var bulkData = new FByteArrayData(payload);

        Assert.Equal(payload, bulkData.Data);
        Assert.False(bulkData.TryReleaseCachedData());
        Assert.Equal(payload, bulkData.Data);
    }
}
