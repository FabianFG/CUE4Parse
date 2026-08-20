using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace CUE4Parse.Tests.Fixtures;

public enum FixtureSerialization
{
    Tagged,
    Unversioned
}

public enum FixtureCompression
{
    Oodle,
    Zlib,
    Uncompressed,
    OodlePartitioned,
    OodleEncrypted
}

internal static class FixtureTestUtilities
{
    private const string FixtureEngineEnvironmentVariable = "CUE4PARSE_FIXTURE_ENGINE";
    private const string FixtureAesKeyText = "CUE4ParseFixtureAESKey0123456789";
    private static readonly FixtureEngineInfo ActiveEngine = ResolveFixtureEngine();
    private static string FixtureRoot => $"Fixtures/{ActiveEngine.DirectoryName}";
    public static EGame FixtureGame => ActiveEngine.Game;
    public static (int Major, int Minor) FixtureEngineVersion =>
        (ActiveEngine.MajorVersion, ActiveEngine.MinorVersion);
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
            "/Game/Fixtures/Audio/SW_Format_ADPCM",
            "/Game/Fixtures/Audio/SW_Format_BinkAudio",
            "/Game/Fixtures/Audio/SW_Format_Opus",
            "/Game/Fixtures/Audio/SW_Format_PCM",
            "/Game/Fixtures/Audio/SW_Format_PlatformSpecific",
            "/Game/Fixtures/Audio/SW_Format_ProjectDefined",
            "/Game/Fixtures/Audio/SW_Format_RADAudio",
            "/Game/Fixtures/Animations/AM_Fixture",
            "/Game/Fixtures/Animations/AS_Fixture",
            "/Game/Fixtures/Animations/AS_Additive",
            "/Game/Fixtures/Animations/BS_Fixture",
            "/Game/Fixtures/Animations/AC_Fixture",
            "/Game/Fixtures/Animations/PA_Fixture",
            "/Game/Fixtures/Animations/SKEL_Fixture",
            "/Game/Fixtures/Blueprints/BP_Fixture",
            "/Game/Fixtures/Blueprints/BP_Components",
            "/Game/Fixtures/Blueprints/BP_ComponentsChild",
            "/Game/Fixtures/Blueprints/E_Fixture",
            "/Game/Fixtures/Blueprints/S_Fixture",
            "/Game/Fixtures/BuildData/BuildData_Fixture",
            "/Game/Fixtures/Compute/CG_Fixture",
            "/Game/Fixtures/ControlRig/CR_Fixture",
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
            "/Game/Fixtures/Effects/MS_Patch",
            "/Game/Fixtures/Effects/MS_Source",
            "/Game/Fixtures/Effects/NS_Fixture",
            "/Game/Fixtures/Effects/VF_Static",
            "/Game/Fixtures/Foliage/FT_Fixture",
            "/Game/Fixtures/Fonts/Font_Fixture",
            "/Game/Fixtures/Geometry/DM_Fixture",
            "/Game/Fixtures/Geometry/GC_Fixture",
            "/Game/Fixtures/Geometry/GeoCache_Fixture",
            "/Game/Fixtures/Landscape/LI_Fixture",
            "/Game/Fixtures/Landscape/M_Landscape",
            "/Game/Fixtures/Maps/Empty",
            "/Game/Fixtures/Maps/Landscape",
            "/Game/Fixtures/Maps/Navigation",
            "/Game/Fixtures/Maps/Instancing",
            "/Game/Fixtures/Maps/Streaming",
            "/Game/Fixtures/Maps/Streaming_Sublevel",
            "/Game/Fixtures/Maps/WorldPartition",
            "/Game/Fixtures/Materials/MI_Fixture",
            "/Game/Fixtures/Materials/M_Fixture",
            "/Game/Fixtures/Materials/MPC_Fixture",
            "/Game/Fixtures/Meshes/SK_Fixture",
            "/Game/Fixtures/Meshes/SM_Fixture",
            "/Game/Fixtures/Meshes/SM_Nanite",
            "/Game/Fixtures/Properties/DA_AllProperties",
            "/Game/Fixtures/Properties/DA_PrimaryAsset",
            "/Game/Fixtures/PoseSearch/PS_Schema",
            "/Game/Fixtures/PoseSearch/PSD_Fixture",
            "/Game/Fixtures/PCG/PCG_Fixture",
            "/Game/Fixtures/PCG/PCG_Parameters",
            "/Game/Fixtures/NNE/NNE_Identity",
            "/Game/Fixtures/AudioAnalysis/Loudness_Fixture",
            "/Game/Fixtures/AudioAnalysis/ConstantQ_Fixture",
            "/Game/Fixtures/Midi/MIDI_Fixture",
            "/Game/Fixtures/Paper2D/Sprite_Fixture",
            "/Game/Fixtures/Physics/PHYS_Fixture",
            "/Game/Fixtures/Sequences/LS_Fixture",
            "/Game/Fixtures/Sequences/LS_Nested",
            "/Game/Fixtures/StringTables/ST_Fixed",
            "/Game/Fixtures/Textures/T_Array",
            "/Game/Fixtures/Textures/T_Cube",
            "/Game/Fixtures/Textures/T_CubeArray",
            "/Game/Fixtures/Textures/T_CurveAtlas",
            "/Game/Fixtures/Textures/T_FloatRGBA",
            "/Game/Fixtures/Textures/T_G16",
            "/Game/Fixtures/Textures/T_IES",
            "/Game/Fixtures/Textures/T_R16F",
            "/Game/Fixtures/Textures/T_UDIM",
            "/Game/Fixtures/Textures/T_Virtual",
            "/Game/Fixtures/Textures/T_Volume",
            "/Game/Fixtures/WorldPartition/DLA_Runtime"
        }
        .Concat(TextureExpectations.Select(static expectation => $"/Game/Fixtures/Textures/{expectation.Asset}"))
        .Append(GetWorldPartitionRuntimeCellPackageName())
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

    private static FixtureEngineInfo ResolveFixtureEngine()
    {
        string configured = Environment.GetEnvironmentVariable(FixtureEngineEnvironmentVariable) ?? "UE5_8";
        return configured.ToUpperInvariant() switch
        {
            "UE5_8" => new("UE5_8", EGame.GAME_UE5_8, 5, 8),
            "UE6_0" => new("UE6_0", EGame.GAME_UE6_0, 6, 0),
            _ => throw new InvalidOperationException(
                $"Unsupported {FixtureEngineEnvironmentVariable} value '{configured}'. Expected UE5_8 or UE6_0.")
        };
    }

    public static string GetWorldPartitionRuntimeCellPackageName()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(FixturePath("manifest.json")));
        return Assert.Single(
            manifest.RootElement.GetProperty("testContainerPackages").EnumerateArray()
                .Select(static element => element.GetString())
                .OfType<string>(),
            static packageName => packageName.Contains(
                "/Maps/WorldPartition/_Generated_/",
                StringComparison.Ordinal));
    }

    public static string[] GetExpectedContainerBulkPaths()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(FixturePath("manifest.json")));
        return manifest.RootElement.GetProperty("testContainerBulkPaths").EnumerateArray()
            .Select(static element => element.GetString())
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static DefaultFileProvider CreateIoStoreProvider(
        FixtureSerialization serialization,
        string mappingFileName = "CUE4ParseFixtures-Oodle.usmap",
        FixtureCompression compression = FixtureCompression.Oodle,
        bool submitEncryptionKey = false)
    {
        var variantDirectory = FixturePath("IoStore", serialization.ToString());
        var containerDirectory = Path.Combine(variantDirectory, compression.ToString());
        Assert.True(Directory.Exists(containerDirectory), $"Missing test fixture directory: {containerDirectory}");

        var globalToc = Path.Combine(variantDirectory, "global.utoc");
        Assert.True(File.Exists(globalToc), $"Missing global test container: {globalToc}");

        var provider = new DefaultFileProvider(
            containerDirectory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(FixtureGame),
            StringComparer.OrdinalIgnoreCase);

        if (serialization == FixtureSerialization.Unversioned)
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(FixturePath("Mappings", mappingFileName));

        provider.Initialize();
        provider.RegisterVfs(globalToc);
        if (compression == FixtureCompression.OodleEncrypted && submitEncryptionKey)
        {
            provider.SubmitKey(default, CreateFixtureAesKey());
        }
        return provider;
    }

    public static FAesKey CreateFixtureAesKey() =>
        new(Encoding.ASCII.GetBytes(FixtureAesKeyText));

    public static DefaultFileProvider CreateMountedIoStoreProvider(
        FixtureSerialization serialization,
        string mappingFileName = "CUE4ParseFixtures-Oodle.usmap",
        FixtureCompression compression = FixtureCompression.Oodle)
    {
        var encrypted = compression == FixtureCompression.OodleEncrypted;
        var provider = CreateIoStoreProvider(serialization, mappingFileName, compression, encrypted);
        if (!encrypted)
            Assert.Equal(1, provider.Mount());
        return provider;
    }

    public static DefaultFileProvider CreateMountedAndroidTextureProvider()
    {
        var directory = FixturePath("AndroidPak");
        Assert.True(Directory.Exists(directory), $"Missing Android texture fixture directory: {directory}");
        var provider = new DefaultFileProvider(
            directory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(FixtureGame),
            StringComparer.OrdinalIgnoreCase);
        provider.Initialize();
        Assert.Equal(1, provider.Mount());
        return provider;
    }

    public static T LoadExport<T>(DefaultFileProvider provider, string packageSuffix, string exportName)
        where T : UObject
    {
        var exports = LoadPackageExports(provider, packageSuffix);
        return Assert.IsAssignableFrom<T>(Assert.Single(exports, export => export.Name == exportName));
    }

    public static UObject[] LoadPackageExports(DefaultFileProvider provider, string packageSuffix)
    {
        var packagePath = Assert.Single(
            provider.Files.Keys.Distinct(StringComparer.OrdinalIgnoreCase),
            path => path.EndsWith(packageSuffix, StringComparison.OrdinalIgnoreCase));
        return provider.LoadPackage(packagePath).GetExports().ToArray();
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

    public static void AssertReferenceSkeleton(FReferenceSkeleton skeleton)
    {
        Assert.Equal(["Root", "Joint_1", "Joint_2"],
            skeleton.FinalRefBoneInfo.Select(bone => bone.Name.Text).ToArray());
        Assert.Equal([-1, 0, 1],
            skeleton.FinalRefBoneInfo.Select(bone => bone.ParentIndex).ToArray());
        Assert.Equal(3, skeleton.FinalRefBonePose.Length);
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

    private readonly record struct FixtureEngineInfo(
        string DirectoryName,
        EGame Game,
        int MajorVersion,
        int MinorVersion);
}
