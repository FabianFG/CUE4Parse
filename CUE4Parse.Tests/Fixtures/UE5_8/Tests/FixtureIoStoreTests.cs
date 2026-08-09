using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureIoStoreTests
{
    private const string PropertyFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/Properties/DA_AllProperties.uasset";
    private const string DataTableFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/DataTables/DT_AllProperties.uasset";
    private const string MapFixtureSuffix = "CUE4ParseFixtures/Content/Fixtures/Maps/Empty.umap";
    private static readonly string[] ExpectedPackagePaths = ExpectedCookedPackageNames
        .Select(static packageName =>
        {
            var extension = packageName == "/Game/Fixtures/Maps/Empty" ? "umap" : "uasset";
            return $"CUE4ParseFixtures/Content/{packageName["/Game/".Length..]}.{extension}";
        })
        .ToArray();

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void IoStoreContainsOnlyPackagesUsedByTests(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        Assert.Equal(
            ExpectedPackagePaths,
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
        Assert.Equal(12, mainChunks.Count(chunk => chunk.ChunkType == (byte) EIoChunkType5.ExportBundleData));
        Assert.Equal(1, mainChunks.Count(chunk => chunk.ChunkType == (byte) EIoChunkType5.ContainerHeader));
        Assert.Equal(13, mainChunks.Length);
        Assert.All(
            mainChunks,
            chunk => Assert.True(
                chunk.ChunkType is (byte) EIoChunkType5.ExportBundleData or (byte) EIoChunkType5.ContainerHeader,
                $"Unexpected chunk type in minimal main container: {(EIoChunkType5) chunk.ChunkType}"));

        var globalChunk = Assert.Single(global.TocResource.ChunkIds);
        Assert.Equal((byte) EIoChunkType5.ScriptObjects, globalChunk.ChunkType);

        Assert.Equal(1, provider.Mount());
        Assert.Equal(12, provider.Files.Count);
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
    public void IoStoreMountsAndLoadsPropertyFixture(
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
        Assert.Equal(0x12345678, fixture.Get<int>("Integer", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0x1122334455667788, fixture.Get<long>("Integer64", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("CUE4Parse_日本語_äöü", fixture.Get<string>("String", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PropertyFixtureDeserializesRuntimeValues(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        var fixture = LoadExport<UObject>(provider, PropertyFixtureSuffix, "DA_AllProperties");

        Assert.Equal(0x0BADF00D, fixture.Get<int>("BaseMarker"));
        Assert.Equal("Inherited_Base_Value", fixture.Get<string>("BaseString"));
        Assert.True(fixture.Get<bool>("bBoolean"));
        Assert.Equal(0xAB, fixture.Get<byte>("Byte"));
        Assert.Equal(0x12345678, fixture.Get<int>("Integer"));
        Assert.Equal(0x1122334455667788, fixture.Get<long>("Integer64"));
        Assert.Equal(1234.25f, GetProperty<float>(fixture, "Float"));
        Assert.Equal(-987654.125, fixture.Get<double>("Double"));
        Assert.Equal("CUE4Parse_日本語_äöü", fixture.Get<string>("String"));
        Assert.Equal("Fixture_Name_0xC0FFEE", fixture.Get<FName>("Name").Text);
        Assert.Equal("Localized-ish fixture text Ω", fixture.Get<FText>("Text").Text);

        AssertVector(fixture.Get<FVector>("Vector"), 1.25f, -2.5f, 3.75f);
        var rotator = fixture.Get<FRotator>("Rotator");
        Assert.Equal(10.0f, rotator.Pitch);
        Assert.Equal(20.0f, rotator.Yaw);
        Assert.Equal(30.0f, rotator.Roll);

        var quat = fixture.Get<FQuat>("Quat");
        Assert.Equal(0.1825741858f, quat.X, 6);
        Assert.Equal(-0.3651483717f, quat.Y, 6);
        Assert.Equal(0.5477225575f, quat.Z, 6);
        Assert.Equal(0.7302967433f, quat.W, 6);

        var transform = fixture.Get<FTransform>("Transform");
        AssertVector(transform.Translation, 100.5f, -200.25f, 300.125f);
        AssertVector(transform.Scale3D, 1.5f, 2.0f, 0.5f);
        Assert.Equal(quat.X, transform.Rotation.X, 6);
        Assert.Equal(quat.Y, transform.Rotation.Y, 6);
        Assert.Equal(quat.Z, transform.Rotation.Z, 6);
        Assert.Equal(quat.W, transform.Rotation.W, 6);

        var guid = fixture.Get<FGuid>("Guid");
        Assert.Equal(0xDEADBEEFu, guid.A);
        Assert.Equal(0x01234567u, guid.B);
        Assert.Equal(0x89ABCDEFu, guid.C);
        Assert.Equal(0x13579BDFu, guid.D);
        Assert.EndsWith("Beta", fixture.Get<FName>("Enum").Text, StringComparison.Ordinal);

        AssertNested(fixture.Get<FStructFallback>("Nested"), 777, "PrimaryNested_日本語", 7, 8, 9, true);
        Assert.Equal([11, 22, 33, 44], fixture.Get<int[]>("IntegerArray"));

        var structArray = fixture.Get<FStructFallback[]>("StructArray");
        Assert.Equal(3, structArray.Length);
        AssertNested(structArray[0], 1, "First", 1, 2, 3, true);
        AssertNested(structArray[1], 2, "Second", -4, 5, -6, false);
        AssertNested(structArray[2], 3, "Third", 7.5f, 8.5f, 9.5f, true);

        var nameSet = fixture.Get<FName[]>("NameSet").Select(name => name.Text).ToHashSet(StringComparer.Ordinal);
        Assert.True(nameSet.SetEquals(["Set_Alpha", "Set_Beta", "Set_日本語"]));

        var integerMap = fixture.Get<Dictionary<FName, int>>("IntegerMap")
            .ToDictionary(pair => pair.Key.Text, pair => pair.Value, StringComparer.Ordinal);
        Assert.Equal(int.MinValue, integerMap["Minimum"]);
        Assert.Equal(42, integerMap["Answer"]);
        Assert.Equal(0x12345678, integerMap["Marker"]);

        var structMap = fixture.Get<Dictionary<FName, FStructFallback>>("StructMap")
            .ToDictionary(pair => pair.Key.Text, pair => pair.Value, StringComparer.Ordinal);
        AssertNested(structMap["Left"], 314, "MapLeft", -1, -2, -3, true);
        AssertNested(structMap["Right"], 271, "MapRight", 4, 5, 6, false);

        Assert.Equal("DT_AllProperties", fixture.Get<FPackageIndex>("HardObjectReference").Name);
        Assert.Equal(
            "/Game/Fixtures/DataTables/DT_AllProperties.DT_AllProperties",
            fixture.Get<FSoftObjectPath>("SoftObjectReference").ToString());
        Assert.Equal("T_BC7", fixture.Get<FPackageIndex>("HardTextureReference").Name);
        Assert.Equal(
            "/Game/Fixtures/Textures/T_BC6H.T_BC6H",
            fixture.Get<FSoftObjectPath>("SoftTextureReference").ToString());
        Assert.Equal("Texture2D", fixture.Get<FPackageIndex>("ClassReference").Name);
        Assert.Equal(
            "/Script/Engine.Texture2D",
            fixture.Get<FSoftObjectPath>("SoftClassReference").ToString());
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PropertyFixtureReferencesResolveAcrossPackages(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        var fixture = LoadExport<UObject>(provider, PropertyFixtureSuffix, "DA_AllProperties");

        var hardTableReference = fixture.Get<FPackageIndex>("HardObjectReference");
        var hardTable = Assert.IsType<UDataTable>(hardTableReference.Load<UDataTable>());
        Assert.Equal("DT_AllProperties", hardTable.Name);
        Assert.True(hardTable.TryGetDataTableRow("Alpha", StringComparison.Ordinal, out var hardAlpha));
        Assert.Equal(0x12345678, hardAlpha.Get<int>("Number"));

        var hardTextureReference = fixture.Get<FPackageIndex>("HardTextureReference");
        var hardTexture = Assert.IsType<UTexture2D>(hardTextureReference.Load<UTexture2D>());
        Assert.Equal("T_BC7", hardTexture.Name);
        Assert.Equal(EPixelFormat.PF_BC7, hardTexture.Format);

        var softTablePath = fixture.Get<FSoftObjectPath>("SoftObjectReference");
        var softTable = provider.LoadPackageObject<UDataTable>(softTablePath.ToString());
        Assert.Equal("DT_AllProperties", softTable.Name);
        Assert.True(softTable.TryGetDataTableRow("Beta", StringComparison.Ordinal, out var softBeta));
        Assert.Equal(-202020, softBeta.Get<int>("Number"));

        var softTexturePath = fixture.Get<FSoftObjectPath>("SoftTextureReference");
        var softTexture = provider.LoadPackageObject<UTexture2D>(softTexturePath.ToString());
        Assert.Equal("T_BC6H", softTexture.Name);
        Assert.Equal(EPixelFormat.PF_BC6H, softTexture.Format);
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

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void DataTableFixtureDeserializesCustomRows(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        var table = LoadExport<UDataTable>(provider, DataTableFixtureSuffix, "DT_AllProperties");
        Assert.Equal("FixtureTableRow", table.RowStructName);
        Assert.Equal(["Alpha", "Beta", "Defaults"], table.RowMap.Keys.Select(name => name.Text).Order().ToArray());

        Assert.True(table.TryGetDataTableRow("Alpha", StringComparison.Ordinal, out var alpha));
        Assert.Equal(0x12345678, alpha.Get<int>("Number"));
        Assert.Equal(0x1122334455667788, alpha.Get<long>("LargeNumber"));
        Assert.Equal("Alpha_CUE4Parse_日本語_äöü", alpha.Get<string>("Message"));
        Assert.EndsWith("Alpha", alpha.Get<FName>("Kind").Text, StringComparison.Ordinal);
        AssertNested(alpha.Get<FStructFallback>("Nested"), 101, "NestedAlpha", 1.25f, -2.5f, 3.75f, true);
        Assert.Equal([11, 22, 33, 44], alpha.Get<int[]>("Values"));

        Assert.True(table.TryGetDataTableRow("Beta", StringComparison.Ordinal, out var beta));
        Assert.Equal(-202020, beta.Get<int>("Number"));
        Assert.Equal(-9007199254740991, beta.Get<long>("LargeNumber"));
        Assert.Equal("Beta_ß_水_🚀", beta.Get<string>("Message"));
        Assert.EndsWith("Beta", beta.Get<FName>("Kind").Text, StringComparison.Ordinal);
        AssertNested(beta.Get<FStructFallback>("Nested"), -202, "NestedBeta", -10, 20, -30, false);
        Assert.Equal([-7, 0, 7, int.MaxValue], beta.Get<int[]>("Values"));

        Assert.True(table.TryGetDataTableRow("Defaults", StringComparison.Ordinal, out var defaults));
        Assert.Equal(0, defaults.GetOrDefault<int>("Number"));
        Assert.Equal(0, defaults.GetOrDefault<long>("LargeNumber"));
        Assert.Equal(string.Empty, defaults.GetOrDefault("Message", string.Empty));
        Assert.EndsWith("None", defaults.GetOrDefault<FName>("Kind").Text, StringComparison.Ordinal);
        Assert.Empty(defaults.GetOrDefault<int[]>("Values", []));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void TextureFixturesDeserializeCookedPlatformData(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        foreach (var expectation in TextureExpectations)
        {
            var name = expectation.Asset;
            var suffix = $"CUE4ParseFixtures/Content/Fixtures/Textures/{name}.uasset";
            var texture = LoadExport<UTexture2D>(provider, suffix, name);

            Assert.Equal(expectation.CookedFormat, texture.Format);
            Assert.Equal(64, texture.PlatformData.SizeX);
            Assert.Equal(64, texture.PlatformData.SizeY);
            Assert.Equal(expectation.MipCount, texture.PlatformData.Mips.Length);
            Assert.Equal(64, texture.PlatformData.Mips[0].SizeX);
            Assert.Equal(64, texture.PlatformData.Mips[0].SizeY);
            Assert.NotNull(texture.GetFirstMip());

            if (name == "T_Mips")
            {
                var expectedDimensions = ExpectedMipDimensions;
                for (var mipIndex = 0; mipIndex < expectedDimensions.Length; mipIndex++)
                {
                    var mip = texture.GetMip(mipIndex);
                    Assert.NotNull(mip);
                    Assert.Equal(expectedDimensions[mipIndex], mip.SizeX);
                    Assert.Equal(expectedDimensions[mipIndex], mip.SizeY);
                    Assert.Equal(1, mip.SizeZ);
                    var bulkData = mip.BulkData?.Data;
                    Assert.NotNull(bulkData);
                    Assert.NotEmpty(bulkData);
                }
            }
        }
    }

    private static T GetProperty<T>(IPropertyHolder holder, string name)
    {
        var found = holder.TryGet<T>(name, out var value, comparisonType: StringComparison.OrdinalIgnoreCase);
        Assert.True(
            found,
            $"Missing property '{name}' or its value cannot be converted to {typeof(T).Name}.");
        return value!;
    }

    private static void AssertNested(
        FStructFallback nested,
        int marker,
        string label,
        float x,
        float y,
        float z,
        bool enabled)
    {
        Assert.Equal(marker, nested.Get<int>("Marker"));
        Assert.Equal(label, nested.Get<string>("Label"));
        AssertVector(nested.Get<FVector>("Position"), x, y, z);
        Assert.Equal([0.125f, -2.5f, 99.75f], nested.Get<float[]>("Samples"));
        Assert.Equal(enabled, nested.Get<bool>("bEnabled"));
    }

    private static void AssertVector(FVector actual, float x, float y, float z)
    {
        Assert.Equal(x, actual.X);
        Assert.Equal(y, actual.Y);
        Assert.Equal(z, actual.Z);
    }

}
