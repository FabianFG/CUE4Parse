using System.Text;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Versions;
using UE4Config.Parsing;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixturePakTests
{
    private const string IniPath = "CUE4ParseFixtures/Config/DefaultGame.ini";
    private const string AssetRegistryPath = "CUE4ParseFixtures/AssetRegistry.bin";
    private const string EnglishLocresPath =
        "CUE4ParseFixtures/Content/Localization/CUE4ParseFixtures/en/CUE4ParseFixtures.locres";
    private const string GermanLocresPath =
        "CUE4ParseFixtures/Content/Localization/CUE4ParseFixtures/de/CUE4ParseFixtures.locres";
    private const string LocresNamespace = "CUE4ParseFixtures.Tests";
    private const string CityGreetingKey = "CityGreeting";
    private static readonly string[] ExpectedCultures = ["en", "de"];
    private static readonly string[] ExpectedPakPaths =
        new[] { AssetRegistryPath, IniPath, GermanLocresPath, EnglishLocresPath }
        .Order(StringComparer.Ordinal)
        .ToArray();
    private static readonly string EnglishCompressionPayload = string.Concat(Enumerable.Repeat(
        "Deterministic localization payload for compression coverage. ",
        96)).TrimEnd();
    private static readonly string GermanCompressionPayload = string.Concat(Enumerable.Repeat(
        "Deterministische Lokalisierungsnutzlast für die Kompressionsprüfung. ",
        96)).TrimEnd();

    [Theory]
    [InlineData(FixtureCompression.Oodle)]
    [InlineData(FixtureCompression.Uncompressed)]
    public void MinimalPakMountsAndDeserializesDeterministicFiles(
        FixtureCompression compression)
    {
        using var provider = CreatePakProvider(compression);
        Assert.Single(provider.UnloadedVfs);
        Assert.Equal(1, provider.Mount());
        Assert.Single(provider.MountedVfs);
        Assert.Equal(
            ExpectedPakPaths,
            provider.Files.Keys.Order(StringComparer.Ordinal).ToArray());

        var expectedCompression = compression == FixtureCompression.Oodle
            ? CompressionMethod.Oodle
            : CompressionMethod.None;
        var iniFile = provider[IniPath];
        Assert.Equal(CompressionMethod.None, iniFile.CompressionMethod);
        Assert.Equal(expectedCompression, provider[AssetRegistryPath].CompressionMethod);
        Assert.Equal(expectedCompression, provider[EnglishLocresPath].CompressionMethod);
        Assert.Equal(expectedCompression, provider[GermanLocresPath].CompressionMethod);

        AssertIni(iniFile.Read());
        AssertLocres(provider[EnglishLocresPath], ELanguage.English);
        AssertLocres(provider[GermanLocresPath], ELanguage.German);
        AssertAssetRegistry(provider);
    }

    [Theory]
    [InlineData(FixtureCompression.Oodle)]
    [InlineData(FixtureCompression.Uncompressed)]
    public void LocalizationManagerChangesLanguageUsingFixedNamespaceAndKey(FixtureCompression compression)
    {
        using var provider = CreateMountedPakProvider(compression);

        Assert.Equal(ExpectedCultures, provider.Internationalization.AvailableCultures);

        provider.ChangeCulture("en");
        Assert.Equal("en", provider.Internationalization.Culture);
        Assert.Equal(
            "Greetings from Frankfurt",
            provider.Internationalization.SafeGet(LocresNamespace, CityGreetingKey));

        provider.ChangeCulture("de");
        Assert.Equal("de", provider.Internationalization.Culture);
        Assert.Equal(
            "Grüße aus Frankfurt",
            provider.Internationalization.SafeGet(LocresNamespace, CityGreetingKey));
    }

    private static DefaultFileProvider CreateMountedPakProvider(FixtureCompression compression)
    {
        var provider = CreatePakProvider(compression);
        Assert.Equal(1, provider.Mount());
        provider.PostMount();
        return provider;
    }

    private static DefaultFileProvider CreatePakProvider(FixtureCompression compression)
    {
        var directory = FixturePath("Pak", compression.ToString());
        Assert.True(Directory.Exists(directory), $"Missing Pak fixture directory: {directory}");
        var provider = new DefaultFileProvider(
            directory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(FixtureGame),
            StringComparer.OrdinalIgnoreCase);
        provider.Initialize();
        return provider;
    }

    private static void AssertIni(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        Assert.DoesNotContain('\r', text);
        Assert.DoesNotContain("Grüße", text, StringComparison.Ordinal);
        Assert.Contains("Text=Greetings from Frankfurt – 日本語 – Ω – 🚀", text, StringComparison.Ordinal);

        var ini = new CustomConfigIni("DefaultGame");
        using var stream = new MemoryStream(data);
        using var reader = new StreamReader(stream, Encoding.UTF8, false);
        ini.Read(reader);

        AssertConfigValue(ini, "Fixture", "String", "Deterministic fixture value");
        AssertConfigValue(ini, "Fixture", "Integer", "305419896");
        AssertConfigValue(ini, "Fixture", "Float", "1234.25");
        AssertConfigValue(ini, "Fixture", "Boolean", "True");
        AssertConfigValue(ini, "Fixture", "Empty", "");
        AssertConfigValue(ini, "Unicode", "Text", "Greetings from Frankfurt – 日本語 – Ω – 🚀");
        AssertConfigValue(ini, "Unicode", "Path", "/Game/Fixtures/Textures/T_BC7.T_BC7");

        var arrayInstructions = new List<InstructionToken>();
        ini.FindPropertyInstructions("Fixture", "Array", arrayInstructions);
        Assert.Equal(["Alpha", "Beta", "日本語"], arrayInstructions.Select(x => x.Value).ToArray());
        Assert.All(arrayInstructions, x => Assert.Equal(InstructionType.Add, x.InstructionType));

        var cultures = new List<InstructionToken>();
        ini.FindPropertyInstructions(
            "/Script/UnrealEd.ProjectPackagingSettings",
            "CulturesToStage",
            cultures);
        Assert.Equal(ExpectedCultures, cultures.Select(x => x.Value).ToArray());
    }

    private static void AssertConfigValue(CustomConfigIni ini, string section, string name, string expected)
    {
        var instructions = new List<InstructionToken>();
        ini.FindPropertyInstructions(section, name, instructions);
        Assert.Equal(expected, Assert.Single(instructions).Value);
    }

    private static void AssertLocres(GameFile locresFile, ELanguage language)
    {
        using var locresReader = locresFile.CreateReader();
        var locres = new FTextLocalizationResource(locresReader);
        var entries = Assert.Single(locres.Entries, x => x.Key.Str == LocresNamespace).Value;
        Assert.Equal(5, entries.Count);
        var isGerman = language switch
        {
            ELanguage.English => false,
            ELanguage.German => true,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
        AssertLocresEntry(
            entries,
            CityGreetingKey,
            isGerman ? "Grüße aus Frankfurt" : "Greetings from Frankfurt");
        AssertLocresEntry(
            entries,
            "Unicode",
            isGerman ? "Unicode-Übersetzung – 日本語 – Ω – 🚀" : "Unicode source text – 日本語 – Ω – 🚀");
        AssertLocresEntry(entries, "SharedFirst", isGerman ? "Gemeinsam verwendete Übersetzung" : "First shared source");
        AssertLocresEntry(entries, "SharedSecond", isGerman ? "Gemeinsam verwendete Übersetzung" : "Second shared source");

        AssertLocresEntry(
            entries,
            "CompressionPayload",
            isGerman ? GermanCompressionPayload : EnglishCompressionPayload);
    }

    private static void AssertAssetRegistry(DefaultFileProvider provider)
    {
        using var registryReader = provider[AssetRegistryPath].CreateReader();
        var registry = new FAssetRegistryState(registryReader);
        var fixturePackages = registry.PreallocatedAssetDataBuffers
            .Select(asset => asset.PackageName.Text)
            .Where(packageName => packageName.StartsWith("/Game/Fixtures/", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedCookedPackageNames, fixturePackages);
        Assert.Contains(
            registry.PreallocatedAssetDataBuffers,
            asset => asset.PackageName.Text == "/Game/Fixtures/Maps/Empty" &&
                     asset.AssetName.Text == "Empty" &&
                     asset.AssetClass.Text == "World");

        var primaryAsset = Assert.Single(registry.PreallocatedAssetDataBuffers,
            asset => asset.PackageName.Text == "/Game/Fixtures/Properties/DA_PrimaryAsset");
        Assert.Equal("FixturePrimaryAsset", primaryAsset.AssetClass.Text);
        Assert.Equal("Primary_Registry_0x13579BDF",
            primaryAsset.TagsAndValues.Single(tag => tag.Key.Text == "RegistryMarker").Value);
        var bundle = Assert.Single(primaryAsset.TaggedAssetBundles.Bundles);
        Assert.Equal("Fixture", bundle.BundleName.Text);
        Assert.Equal("/Game/Fixtures/Properties/DA_AllProperties.DA_AllProperties",
            Assert.Single(bundle.BundleAssets).AssetPathName.Text);

        var primaryNode = Assert.Single(registry.PreallocatedDependsNodeDataBuffers,
            node => node.Identifier?.PackageName.Text == primaryAsset.PackageName.Text);
        Assert.Contains(primaryNode.PackageDependencies,
            index => registry.PreallocatedDependsNodeDataBuffers[index].Identifier?.PackageName.Text ==
                     "/Game/Fixtures/Properties/DA_AllProperties");
        var propertyNode = Assert.Single(registry.PreallocatedDependsNodeDataBuffers,
            node => node.Identifier?.PackageName.Text == "/Game/Fixtures/Properties/DA_AllProperties");
        var dataTableDependency = FindPackageDependency(
            registry, propertyNode, "/Game/Fixtures/DataTables/DT_AllProperties");
        Assert.Contains(UnpackPackageProperties(propertyNode, dataTableDependency),
            static properties => (properties & 1) != 0);
        var softTextureDependency = FindPackageDependency(
            registry, propertyNode, "/Game/Fixtures/Textures/T_BC6H");
        Assert.All(UnpackPackageProperties(propertyNode, softTextureDependency),
            static properties => Assert.Equal(0, properties & 1));
        var primaryNodeIndex = Array.IndexOf(registry.PreallocatedDependsNodeDataBuffers, primaryNode);
        Assert.Contains(propertyNode.Referencers, index => index == primaryNodeIndex);

        var packageData = Assert.Single(registry.PreallocatedPackageDataBuffers,
            package => package.PackageName.Text == primaryAsset.PackageName.Text);
        Assert.True(packageData.DiskSize > 0);
        Assert.Equal(".uasset", Assert.IsType<string>(packageData.ExtensionText).TrimEnd('\0'));
        Assert.NotEmpty(packageData.ImportedClasses ?? []);
    }

    private static int FindPackageDependency(
        FAssetRegistryState registry,
        FDependsNode node,
        string packageName)
    {
        var dependency = Assert.Single(node.PackageDependencies.Select((nodeIndex, dependencyIndex) =>
                (NodeIndex: nodeIndex, DependencyIndex: dependencyIndex)),
            candidate => registry.PreallocatedDependsNodeDataBuffers[candidate.NodeIndex]
                .Identifier?.PackageName.Text == packageName);
        return dependency.DependencyIndex;
    }

    private static int[] UnpackPackageProperties(FDependsNode node, int dependencyIndex)
    {
        var packed = 0;
        for (var bit = 0; bit < 5; ++bit)
        {
            if (node.PackageFlags[dependencyIndex * 5 + bit]) packed |= 1 << bit;
        }
        return packed switch
        {
            < 8 => [packed],
            8 => [1, 2],
            9 => [1, 4],
            10 => [1, 6],
            11 => [2, 4],
            12 => [2, 5],
            13 => [3, 4],
            14 => [3, 5],
            15 => [3, 6],
            16 => [5, 6],
            17 => [1, 2, 4],
            18 => [3, 5, 6],
            _ => throw new InvalidDataException($"Invalid packed package property set {packed}")
        };
    }

    private static void AssertLocresEntry(
        IReadOnlyDictionary<FTextKey, FEntry> entries,
        string key,
        string expectedTranslation)
    {
        var entry = Assert.Single(entries, x => x.Key.Str == key).Value;
        Assert.Equal(expectedTranslation, entry.LocalizedString);
        Assert.NotEqual(0u, entry.SourceStringHash);
    }
}
