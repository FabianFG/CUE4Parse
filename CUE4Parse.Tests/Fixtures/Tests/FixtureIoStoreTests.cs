using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureIoStoreTests
{
    private const string PropertyFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/Properties/DA_AllProperties.uasset";
    private const string MapFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/Maps/Empty.umap";
    private static readonly string[] ExpectedBulkPaths = GetExpectedContainerBulkPaths();
    private static readonly string[] ExpectedContainerPaths = ExpectedCookedPackageNames
        .Select(static packageName =>
        {
            var extension = packageName.StartsWith("/Game/Fixtures/Maps/", StringComparison.Ordinal)
                ? "umap"
                : "uasset";
            return $"CUE4ParseFixtures/Content/{packageName["/Game/".Length..]}.{extension}";
        })
        .Concat(ExpectedBulkPaths)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void IoStoreContainsOnlyPackagesUsedByTests(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        Assert.Equal(
            ExpectedContainerPaths,
            provider.Files.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Oodle)]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Zlib)]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Uncompressed)]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.OodlePartitioned)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Oodle)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Zlib)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Uncompressed)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.OodlePartitioned)]
    public void IoStoreContainsOnlyExpectedChunkTypesAndCompression(
        FixtureSerialization serialization,
        FixtureCompression compression)
    {
        using var provider = CreateIoStoreProvider(serialization, compression: compression);
        Assert.Equal(2, provider.UnloadedVfs.Count);
        Assert.All(provider.UnloadedVfs, reader => Assert.IsType<IoStoreReader>(reader));

        var readers = provider.UnloadedVfs.OfType<IoStoreReader>();
        var expectedMainName = $"CUE4ParseFixtures-Minimal-{compression}-Windows.utoc";
        var main = Assert.Single(
            readers,
            reader => Path.GetFileName(reader.Name).Equals(
                expectedMainName,
                StringComparison.OrdinalIgnoreCase));
        var global = Assert.Single(
            readers,
            reader => Path.GetFileName(reader.Name).Equals("global.utoc", StringComparison.OrdinalIgnoreCase));

        var expectedCompression = compression switch
        {
            FixtureCompression.Oodle or FixtureCompression.OodlePartitioned => CompressionMethod.Oodle,
            FixtureCompression.Zlib => CompressionMethod.Zlib,
            _ => CompressionMethod.None
        };
        Assert.Equal(
            expectedCompression != CompressionMethod.None,
            main.TocResource.Header.ContainerFlags.HasFlag(EIoContainerFlags.Compressed));
        Assert.True(main.TocResource.Header.ContainerFlags.HasFlag(EIoContainerFlags.Indexed));
        Assert.Equal(
            compression == FixtureCompression.OodlePartitioned,
            main.TocResource.Header.PartitionCount > 1);
        if (expectedCompression != CompressionMethod.None)
        {
            Assert.Contains(expectedCompression, main.TocResource.CompressionMethods);
            Assert.Contains(main.TocResource.CompressionBlocks, block => block.CompressionMethodIndex != 0);
        }
        else
        {
            Assert.All(main.TocResource.CompressionBlocks, block => Assert.Equal(0, block.CompressionMethodIndex));
        }

        var mainChunks = main.TocResource.ChunkIds;
        Assert.Equal(
            ExpectedCookedPackageNames.Length,
            mainChunks.Count(chunk => chunk.ChunkType == (byte) EIoChunkType5.ExportBundleData));
        Assert.Equal(
            ExpectedBulkPaths.Length,
            mainChunks.Count(chunk => chunk.ChunkType == (byte) EIoChunkType5.BulkData));
        Assert.Equal(1, mainChunks.Count(chunk => chunk.ChunkType == (byte) EIoChunkType5.ContainerHeader));
        Assert.Equal(ExpectedContainerPaths.Length + 1, mainChunks.Length);
        Assert.All(
            mainChunks,
            chunk => Assert.True(
                chunk.ChunkType is (byte) EIoChunkType5.ExportBundleData or
                    (byte) EIoChunkType5.BulkData or
                    (byte) EIoChunkType5.ContainerHeader,
                $"Unexpected chunk type in minimal main container: {(EIoChunkType5) chunk.ChunkType}"));

        var globalChunk = Assert.Single(global.TocResource.ChunkIds);
        Assert.Equal((byte) EIoChunkType5.ScriptObjects, globalChunk.ChunkType);

        Assert.Equal(1, provider.Mount());
        Assert.Equal(ExpectedContainerPaths.Length, provider.Files.Count);
        Assert.All(provider.Files.Values, file => Assert.Equal(expectedCompression, file.CompressionMethod));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PartitionedIoStoreReadsAChunkFromANonZeroPartition(FixtureSerialization serialization)
    {
        using var provider = CreateIoStoreProvider(
            serialization, compression: FixtureCompression.OodlePartitioned);
        var reader = Assert.Single(provider.UnloadedVfs.OfType<IoStoreReader>(),
            static candidate => !Path.GetFileName(candidate.Name)
                .Equals("global.utoc", StringComparison.OrdinalIgnoreCase));
        Assert.True(reader.TocResource.Header.PartitionCount > 1);

        var chunk = reader.TocResource.ChunkIds
            .Select((id, index) => (Id: id, Offset: reader.TocResource.ChunkOffsetLengths[index].Offset))
            .First(candidate =>
            {
                var blockIndex = (int) (candidate.Offset / reader.TocResource.Header.CompressionBlockSize);
                return (ulong) reader.TocResource.CompressionBlocks[blockIndex].Offset >=
                       reader.TocResource.Header.PartitionSize;
            });
        Assert.NotEmpty(reader.Read(chunk.Id));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MapFixtureDeserializesWorldAndPersistentLevel(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        var packagePath = Assert.Single(
            provider.Files.Keys,
            path => path.EndsWith(MapFixtureSuffix, StringComparison.OrdinalIgnoreCase));
        var package = provider.LoadPackage(packagePath);
        var world = Assert.Single(package.GetExports().OfType<UWorld>());

        Assert.Equal("Empty", world.Name);
        Assert.True(package.HasFlags(EPackageFlags.PKG_Cooked));
        Assert.True(package.HasFlags(EPackageFlags.PKG_ContainsMap));
        Assert.True(package.HasFlags(EPackageFlags.PKG_FilterEditorOnly));
        Assert.Equal(
            serialization == FixtureSerialization.Unversioned,
            package.HasFlags(EPackageFlags.PKG_UnversionedProperties));
        Assert.True(world.PersistentLevel.IsExport);
        var level = Assert.IsType<ULevel>(world.PersistentLevel.Load<ULevel>());
        Assert.Equal("PersistentLevel", level.Name);
        Assert.NotNull(level.Actors);
        Assert.Empty(world.StreamingLevels);

        var actorReferences = level.Actors.Where(index => index is { IsNull: false }).ToArray();
        Assert.NotEmpty(actorReferences);
        var actors = actorReferences.Select(index => index!.Load<AActor>()).ToArray();
        Assert.DoesNotContain(actors, actor => actor is null);
        Assert.Contains(actors, actor => actor is AWorldSettings);

        Assert.False(level.WorldSettings.IsNull);
        Assert.IsType<AWorldSettings>(level.WorldSettings.Load<AWorldSettings>());
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Oodle)]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Zlib)]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Uncompressed)]
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.OodlePartitioned)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Oodle)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Zlib)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Uncompressed)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.OodlePartitioned)]
    public void IoStoreMountsAndLoadsPackageAcrossCompressionVariants(
        FixtureSerialization serialization,
        FixtureCompression compression)
    {
        using var provider = CreateIoStoreProvider(serialization, compression: compression);

        Assert.Equal(2, provider.UnloadedVfs.Count);
        Assert.Equal(1, provider.Mount());
        Assert.NotNull(provider.GlobalData);
        Assert.Single(provider.MountedVfs);

        var packagePath = Assert.Single(
            provider.Files.Keys,
            path => path.EndsWith(PropertyFixtureSuffix, StringComparison.OrdinalIgnoreCase));
        var package = provider.LoadPackage(packagePath);
        var fixture = Assert.Single(package.GetExports(), export => export.Name == "DA_AllProperties");

        Assert.Equal("ParserFixtureData", fixture.ExportType);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void EncryptedIoStoreRejectsWrongKeyAndLoadsWithFixtureKey(FixtureSerialization serialization)
    {
        using var provider = CreateIoStoreProvider(
            serialization, compression: FixtureCompression.OodleEncrypted);
        var main = Assert.Single(provider.UnloadedVfs, reader => reader is IoStoreReader { IsEncrypted: true });
        Assert.True(((IoStoreReader) main).TocResource.Header.ContainerFlags.HasFlag(EIoContainerFlags.Encrypted));

        Assert.Equal(0, provider.SubmitKey(default, new CUE4Parse.Encryption.Aes.FAesKey(new byte[32])));
        Assert.Empty(provider.Files);

        Assert.Equal(1, provider.SubmitKey(default, CreateFixtureAesKey()));
        Assert.NotNull(provider.GlobalData);
        var fixture = LoadExport<UObject>(provider, PropertyFixtureSuffix, "DA_AllProperties");
        Assert.Equal(0x12345678, fixture.Get<int>("Integer"));
    }

    [Theory]
    [InlineData("CUE4ParseFixtures-Oodle.usmap")]
    [InlineData("CUE4ParseFixtures-Uncompressed.usmap")]
    public void UnversionedFixtureLoadsWithEitherMappingEncoding(string mappingFileName)
    {
        using var provider = CreateMountedIoStoreProvider(FixtureSerialization.Unversioned, mappingFileName);

        var fixture = LoadExport<UObject>(provider, PropertyFixtureSuffix, "DA_AllProperties");
        Assert.Equal(0x12345678, fixture.Get<int>("Integer"));
        AssertNested(fixture.Get<FStructFallback>("Nested"), 777, "PrimaryNested_日本語", 7, 8, 9, true);
    }

}
