using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureLegacyPakTests
{
    private const string PropertySuffix =
        "CUE4ParseFixtures/Content/Fixtures/Properties/DA_AllProperties.uasset";

    public static TheoryData<FixtureSerialization, FixtureCompression> Variants => new()
    {
        { FixtureSerialization.Tagged, FixtureCompression.Oodle },
        { FixtureSerialization.Tagged, FixtureCompression.Zlib },
        { FixtureSerialization.Tagged, FixtureCompression.Uncompressed },
        { FixtureSerialization.Tagged, FixtureCompression.OodleEncrypted },
        { FixtureSerialization.Unversioned, FixtureCompression.Oodle },
        { FixtureSerialization.Unversioned, FixtureCompression.Zlib },
        { FixtureSerialization.Unversioned, FixtureCompression.Uncompressed },
        { FixtureSerialization.Unversioned, FixtureCompression.OodleEncrypted }
    };

    [Theory]
    [MemberData(nameof(Variants))]
    public void LegacyPakMountsAndLoadsSplitPackages(
        FixtureSerialization serialization,
        FixtureCompression compression)
    {
        using var provider = CreateProvider(serialization, compression);
        Mount(provider, compression);

        var fixture = LoadExport<UObject>(provider, PropertySuffix, "DA_AllProperties");
        Assert.Equal(0x12345678, fixture.Get<int>("Integer"));
        Assert.Equal("Fixture_ANSI_123", fixture.Get<string>("AnsiString"));
        Assert.Equal("/Game/Fixtures/Textures/T_BC6H.T_BC6H",
            fixture.Get<FSoftObjectPath>("SoftTextureReference").ToString());
        Assert.Equal(EPixelFormat.PF_BC6H, provider.LoadPackageObject<UTexture2D>(
            "/Game/Fixtures/Textures/T_BC6H.T_BC6H").Format);

        var tableReference = fixture.Get<FPackageIndex>("HardObjectReference");
        var table = Assert.IsType<UDataTable>(tableReference.Load<UDataTable>());
        Assert.True(table.TryGetDataTableRow("Alpha", StringComparison.Ordinal, out var alpha));
        Assert.Equal(0x12345678, alpha.Get<int>("Number"));

        var texture = provider.LoadPackageObject<UTexture2D>(
            "/Game/Fixtures/Textures/T_Streaming.T_Streaming");
        Assert.Equal((512, 512, EPixelFormat.PF_DXT1),
            (texture.PlatformData.SizeX, texture.PlatformData.SizeY, texture.Format));
        var mip = Assert.IsType<FTexture2DMipMap>(texture.GetFirstMip());
        Assert.NotEmpty(Assert.IsType<byte[]>(mip.BulkData?.Data));

        var map = provider.LoadPackage("CUE4ParseFixtures/Content/Fixtures/Maps/Empty.umap");
        var world = Assert.Single(map.GetExports().OfType<UWorld>());
        Assert.Equal("Empty", world.Name);
        Assert.IsType<ULevel>(world.PersistentLevel.Load());

        using var ioStore = CreateMountedIoStoreProvider(serialization);
        var ioFixture = LoadExport<UObject>(ioStore, PropertySuffix, "DA_AllProperties");
        Assert.Equal(fixture.Get<int>("Integer"), ioFixture.Get<int>("Integer"));
        Assert.Equal(fixture.Get<string>("String"), ioFixture.Get<string>("String"));
        Assert.Equal(fixture.Get<FName>("Name"), ioFixture.Get<FName>("Name"));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void EncryptedLegacyPakRejectsWrongKey(FixtureSerialization serialization)
    {
        using var provider = CreateProvider(serialization, FixtureCompression.OodleEncrypted);
        Assert.Equal(0, provider.SubmitKey(default, new FAesKey(new byte[32])));
        Assert.Empty(provider.Files);
        Assert.Equal(1, provider.SubmitKey(default, CreateFixtureAesKey()));
        Assert.NotEmpty(provider.Files);
    }

    private static DefaultFileProvider CreateProvider(
        FixtureSerialization serialization,
        FixtureCompression compression)
    {
        var directory = FixturePath("LegacyPak", serialization.ToString(), compression.ToString());
        Assert.True(Directory.Exists(directory), $"Missing legacy Pak fixture directory: {directory}");
        var provider = new DefaultFileProvider(
            directory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(FixtureGame),
            StringComparer.OrdinalIgnoreCase);
        if (serialization == FixtureSerialization.Unversioned)
        {
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(
                FixturePath("Mappings", "CUE4ParseFixtures-Oodle.usmap"));
        }
        provider.Initialize();
        return provider;
    }

    private static void Mount(DefaultFileProvider provider, FixtureCompression compression)
    {
        var mounted = compression == FixtureCompression.OodleEncrypted
            ? provider.SubmitKey(default, CreateFixtureAesKey())
            : provider.Mount();
        Assert.Equal(1, mounted);
    }
}
