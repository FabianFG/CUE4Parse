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
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureIoStoreTests
{
    private const string PropertyFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/Properties/DA_AllProperties.uasset";
    private const string MapFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/Maps/Empty.umap";
    private static readonly string[] ExpectedBulkPaths =
    [
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Inline.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Streaming.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_ADPCM.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_BinkAudio.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_Opus.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_PCM.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_PlatformSpecific.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_ProjectDefined.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Audio/SW_Format_RADAudio.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Meshes/SM_Nanite.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Textures/T_Streaming.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Textures/T_UDIM.ubulk",
        "CUE4ParseFixtures/Content/Fixtures/Textures/T_Virtual.ubulk"
    ];
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
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Uncompressed)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Oodle)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Uncompressed)]
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

        var expectedCompression = compression == FixtureCompression.Oodle
            ? CompressionMethod.Oodle
            : CompressionMethod.None;
        Assert.Equal(
            expectedCompression == CompressionMethod.Oodle,
            main.TocResource.Header.ContainerFlags.HasFlag(EIoContainerFlags.Compressed));
        Assert.True(main.TocResource.Header.ContainerFlags.HasFlag(EIoContainerFlags.Indexed));
        if (expectedCompression == CompressionMethod.Oodle)
        {
            Assert.Contains(CompressionMethod.Oodle, main.TocResource.CompressionMethods);
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
    [InlineData(FixtureSerialization.Tagged, FixtureCompression.Uncompressed)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Oodle)]
    [InlineData(FixtureSerialization.Unversioned, FixtureCompression.Uncompressed)]
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
