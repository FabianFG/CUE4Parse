using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
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
        new("T_BC1", "rgb.png", 64, 64, EPixelFormat.PF_DXT1, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 1.5, 8),
        new("T_BC3", "rgba.png", 64, 64, EPixelFormat.PF_DXT5, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 1.5, 20),
        new("T_BC4", "grayscale.png", 64, 64, EPixelFormat.PF_BC4, EPixelFormat.PF_B8G8R8A8, SKColorType.Bgra8888, 1, 1.5, 12),
        new("T_BC5", "normal.png", 64, 64, EPixelFormat.PF_BC5, EPixelFormat.PF_B8G8R8A8, SKColorType.Bgra8888, 1, 1.0, 4),
        new("T_BC6H", "hdr.exr", 64, 64, EPixelFormat.PF_BC6H, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 2.0, 80),
        new("T_BC7", "rgba.png", 64, 64, EPixelFormat.PF_BC7, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 1, 1.0, 20),
        new("T_BGRA8", "rgba.png", 64, 64, EPixelFormat.PF_B8G8R8A8, EPixelFormat.PF_B8G8R8A8, SKColorType.Bgra8888, 1, 1.0, 20),
        new("T_G8", "grayscale.png", 64, 64, EPixelFormat.PF_G8, EPixelFormat.PF_G8, SKColorType.Gray8, 1, 0.0, 0),
        new("T_Mips", "rgb.png", 64, 64, EPixelFormat.PF_DXT1, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 7, 1.5, 8),
        new("T_Streaming", "streaming.png", 512, 512, EPixelFormat.PF_DXT1, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888, 10, 1.5, 40)
    ];

    public static readonly string[] ExpectedCookedPackageNames =
        new[]
        {
            "/Game/Fixtures/Audio/SC_Fixture",
            "/Game/Fixtures/Audio/SW_Inline",
            "/Game/Fixtures/Audio/SW_Streaming",
            "/Game/Fixtures/Curves/CT_Composite",
            "/Game/Fixtures/Curves/CT_Rich",
            "/Game/Fixtures/Curves/CT_Simple",
            "/Game/Fixtures/Curves/CT_SimpleOverrides",
            "/Game/Fixtures/Curves/Curve_Float",
            "/Game/Fixtures/Curves/Curve_LinearColor",
            "/Game/Fixtures/Curves/Curve_Vector",
            "/Game/Fixtures/DataTables/DT_AllProperties",
            "/Game/Fixtures/DataTables/DT_Composite",
            "/Game/Fixtures/DataTables/DT_Overrides",
            "/Game/Fixtures/Maps/Empty",
            "/Game/Fixtures/Materials/MI_Fixture",
            "/Game/Fixtures/Materials/M_Fixture",
            "/Game/Fixtures/Meshes/SM_Fixture",
            "/Game/Fixtures/Properties/DA_AllProperties",
            "/Game/Fixtures/StringTables/ST_Fixed",
            "/Game/Fixtures/Textures/T_Array",
            "/Game/Fixtures/Textures/T_Cube",
            "/Game/Fixtures/Textures/T_Volume"
        }
        .Concat(TextureExpectations.Select(static expectation => $"/Game/Fixtures/Textures/{expectation.Asset}"))
        .Order(StringComparer.Ordinal)
        .ToArray();

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

    public static T GetProperty<T>(IPropertyHolder holder, string name)
    {
        var found = holder.TryGet<T>(name, out var value, comparisonType: StringComparison.OrdinalIgnoreCase);
        Assert.True(found, $"Missing property '{name}' or its value cannot be converted to {typeof(T).Name}.");
        return value!;
    }

    public static FPropertyTag GetPropertyTag(IPropertyHolder holder, string name) => Assert.Single(
        holder.Properties,
        property => property.Name.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static void AssertNested(
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

    public static void AssertVector(FVector actual, float x, float y, float z)
    {
        Assert.Equal(x, actual.X);
        Assert.Equal(y, actual.Y);
        Assert.Equal(z, actual.Z);
    }

    internal readonly record struct TextureExpectation(
        string Asset,
        string Source,
        int Width,
        int Height,
        EPixelFormat CookedFormat,
        EPixelFormat DecodedFormat,
        SKColorType SkiaColorType,
        int MipCount,
        double MaximumMeanError,
        int MaximumPixelError);
}
