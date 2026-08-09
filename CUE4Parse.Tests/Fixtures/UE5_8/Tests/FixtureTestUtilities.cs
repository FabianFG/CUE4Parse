using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public enum FixtureSerialization
{
    Tagged,
    Unversioned
}

public enum FixtureCompression
{
    Oodle,
    Uncompressed
}

internal static class FixtureTestUtilities
{
    private const string FixtureRoot = "Fixtures/UE5_8";
    public static readonly TextureExpectation[] TextureExpectations =
    [
        new("T_BC1", "rgb.png", EPixelFormat.PF_DXT1, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 1.5, 8),
        new("T_BC3", "rgba.png", EPixelFormat.PF_DXT5, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 1.5, 20),
        new("T_BC4", "grayscale.png", EPixelFormat.PF_BC4, EPixelFormat.PF_B8G8R8A8, SKColorType.Bgra8888, 1, 1.5, 12),
        new("T_BC5", "normal.png", EPixelFormat.PF_BC5, EPixelFormat.PF_B8G8R8A8, SKColorType.Bgra8888, 1, 1.0, 4),
        new("T_BC6H", "hdr.exr", EPixelFormat.PF_BC6H, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 2.0, 80),
        new("T_BC7", "rgba.png", EPixelFormat.PF_BC7, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 1.0, 20),
        new("T_BGRA8", "rgba.png", EPixelFormat.PF_B8G8R8A8, EPixelFormat.PF_B8G8R8A8, SKColorType.Bgra8888, 1, 1.0, 20),
        new("T_G8", "grayscale.png", EPixelFormat.PF_G8, EPixelFormat.PF_G8, SKColorType.Gray8, 1, 0.0, 0),
        new("T_Mips", "rgb.png", EPixelFormat.PF_DXT1, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 7, 1.5, 8)
    ];

    public static readonly string[] ExpectedCookedPackageNames =
    [
        "/Game/Fixtures/DataTables/DT_AllProperties",
        "/Game/Fixtures/Maps/Empty",
        "/Game/Fixtures/Properties/DA_AllProperties",
        .. TextureExpectations.Select(static expectation => $"/Game/Fixtures/Textures/{expectation.Asset}")
    ];

    public static ReadOnlySpan<int> ExpectedMipDimensions => [64, 32, 16, 8, 4, 2, 1];

    [ModuleInitializer]
    internal static void ConfigureTextureDecoder() =>
        TextureDecoder.UseAssetRipperTextureDecoder = true;

    public static string FixturePath(params ReadOnlySpan<string> components)
    {
        ReadOnlySpan<string> paths = [AppContext.BaseDirectory, FixtureRoot, .. components];
        return Path.Combine(paths);
    }

    public static DefaultFileProvider CreateIoStoreProvider(
        FixtureSerialization serialization,
        string mappingFileName = "CUE4ParseFixtures-Oodle.usmap",
        FixtureCompression compression = FixtureCompression.Oodle)
    {
        var variantDirectory = FixturePath("IoStore", serialization.ToString());
        var containerDirectory = Path.Combine(variantDirectory, compression.ToString());
        Assert.True(Directory.Exists(containerDirectory), $"Missing test fixture directory: {containerDirectory}");

        var globalToc = Path.Combine(variantDirectory, "global.utoc");
        Assert.True(File.Exists(globalToc), $"Missing global test container: {globalToc}");

        var provider = new DefaultFileProvider(
            containerDirectory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(EGame.GAME_UE5_8),
            StringComparer.OrdinalIgnoreCase);

        if (serialization == FixtureSerialization.Unversioned)
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(FixturePath("Mappings", mappingFileName));

        provider.Initialize();
        provider.RegisterVfs(globalToc);
        return provider;
    }

    public static DefaultFileProvider CreateMountedIoStoreProvider(
        FixtureSerialization serialization,
        string mappingFileName = "CUE4ParseFixtures-Oodle.usmap",
        FixtureCompression compression = FixtureCompression.Oodle)
    {
        var provider = CreateIoStoreProvider(serialization, mappingFileName, compression);
        Assert.Equal(1, provider.Mount());
        return provider;
    }

    public static T LoadExport<T>(DefaultFileProvider provider, string packageSuffix, string exportName)
        where T : UObject
    {
        var packagePath = Assert.Single(
            provider.Files.Keys,
            path => path.EndsWith(packageSuffix, StringComparison.OrdinalIgnoreCase));
        var package = provider.LoadPackage(packagePath);
        return Assert.IsAssignableFrom<T>(Assert.Single(package.GetExports(), export => export.Name == exportName));
    }

    internal readonly record struct TextureExpectation(
        string Asset,
        string Source,
        EPixelFormat CookedFormat,
        EPixelFormat DecodedFormat,
        SKColorType SkiaColorType,
        int MipCount,
        double MaximumMeanError,
        int MaximumPixelError);
}
