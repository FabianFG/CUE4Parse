using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixturePatchPrecedenceTests
{
    private enum PatchContainerType
    {
        Pak,
        IoStore
    }

    private const string TargetPackageName = "/Game/PatchFixtures/DA_PatchTarget";
    private const string TargetPackageSuffix =
        "CUE4ParseFixtures/Content/PatchFixtures/DA_PatchTarget.uasset";
    private const string ReferencerPackageSuffix =
        "CUE4ParseFixtures/Content/PatchFixtures/DA_PatchReferencer.uasset";

    [Fact]
    public void PakPatchOverridesDirectHardAndSoftAssetLoads()
    {
        using var provider = CreatePatchProvider(PatchContainerType.Pak);
        MountContainer(provider, PatchContainerType.Pak, patched: false);
        AssertBaseTarget(provider);

        MountContainer(provider, PatchContainerType.Pak, patched: true);
        AssertPatchedTargetAndReferences(provider);
        AssertSelectedArchive(provider, TargetPackageSuffix, "CUE4ParseFixtures-PatchBase_0_P.pak");
    }

    [Fact]
    public void PakPatchPrecedenceIsIndependentOfMountOrder()
    {
        using var provider = CreatePatchProvider(PatchContainerType.Pak);
        MountContainer(provider, PatchContainerType.Pak, patched: true);
        MountContainer(provider, PatchContainerType.Pak, patched: false);

        AssertPatchedTargetAndReferences(provider);
        AssertSelectedArchive(provider, TargetPackageSuffix, "CUE4ParseFixtures-PatchBase_0_P.pak");
    }

    [Fact]
    public void IoStorePatchOverridesDirectHardAndSoftAssetLoads()
    {
        using var provider = CreatePatchProvider(PatchContainerType.IoStore);
        MountContainer(provider, PatchContainerType.IoStore, patched: false);
        AssertBaseTarget(provider);

        MountContainer(provider, PatchContainerType.IoStore, patched: true);
        AssertPatchedTargetAndReferences(provider);
        AssertSelectedArchive(provider, TargetPackageSuffix, "CUE4ParseFixtures-PatchBase_0_P.utoc");

        var packageEntry = provider.FilesById[FPackageId.FromName(TargetPackageName)];
        AssertSelectedArchive(packageEntry, "CUE4ParseFixtures-PatchBase_0_P.utoc");
    }

    [Fact(Skip = "CUE-PARSER-004: FilesById currently overwrites package IDs in mount order instead of read priority.")]
    public void IoStorePatchPackageIdPrecedenceIsIndependentOfMountOrder()
    {
        using var provider = CreatePatchProvider(PatchContainerType.IoStore);
        MountContainer(provider, PatchContainerType.IoStore, patched: true);
        MountContainer(provider, PatchContainerType.IoStore, patched: false);

        AssertPatchedTargetAndReferences(provider);
        AssertSelectedArchive(provider.FilesById[FPackageId.FromName(TargetPackageName)],
            "CUE4ParseFixtures-PatchBase_0_P.utoc");
    }

    private static DefaultFileProvider CreatePatchProvider(PatchContainerType containerType)
    {
        var directory = FixturePath("Patch", containerType.ToString());
        Assert.True(Directory.Exists(directory), $"Missing patch fixture directory: {directory}");
        var provider = new DefaultFileProvider(
            directory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(FixtureGame),
            StringComparer.OrdinalIgnoreCase);

        if (containerType == PatchContainerType.IoStore)
        {
            provider.RegisterVfs(FixturePath("IoStore", "Tagged", "global.utoc"));
        }

        return provider;
    }

    private static void MountContainer(
        DefaultFileProvider provider,
        PatchContainerType containerType,
        bool patched)
    {
        var extension = containerType == PatchContainerType.Pak ? ".pak" : ".utoc";
        var suffix = patched ? "_0_P" : string.Empty;
        provider.RegisterVfs(FixturePath("Patch", containerType.ToString(),
            $"CUE4ParseFixtures-PatchBase{suffix}{extension}"));
        Assert.Equal(1, provider.Mount());
    }

    private static void AssertBaseTarget(DefaultFileProvider provider)
    {
        var target = LoadExport<UObject>(provider, TargetPackageSuffix, "DA_PatchTarget");
        AssertTarget(target, 0x11111111, "Base version", 100, "Base payload");
    }

    private static void AssertPatchedTargetAndReferences(DefaultFileProvider provider)
    {
        var target = LoadExport<UObject>(provider, TargetPackageSuffix, "DA_PatchTarget");
        AssertTarget(target, 0x22222222, "Patch version", 200, "Patch payload");

        var referencer = LoadExport<UObject>(provider, ReferencerPackageSuffix, "DA_PatchReferencer");
        AssertTarget(referencer, 0x33333333, "Unchanged base referencer", 300,
            "Resolves the target through a package import");

        var hardTarget = Assert.IsAssignableFrom<UObject>(referencer.Get<FPackageIndex>("HardObjectReference").Load());
        AssertTarget(hardTarget, 0x22222222, "Patch version", 200, "Patch payload");

        var softTargetPath = referencer.Get<FSoftObjectPath>("SoftObjectReference").ToString();
        Assert.Equal("/Game/PatchFixtures/DA_PatchTarget.DA_PatchTarget", softTargetPath);
        var softTarget = provider.LoadPackageObject<UObject>(softTargetPath);
        AssertTarget(softTarget, 0x22222222, "Patch version", 200, "Patch payload");
    }

    private static void AssertTarget(UObject asset, int baseMarker, string baseString, int integer, string text)
    {
        Assert.Equal(baseMarker, asset.Get<int>("BaseMarker"));
        Assert.Equal(baseString, asset.Get<string>("BaseString"));
        Assert.Equal(integer, asset.Get<int>("Integer"));
        Assert.Equal(text, asset.Get<string>("String"));
    }

    private static void AssertSelectedArchive(
        DefaultFileProvider provider,
        string packageSuffix,
        string expectedArchive)
    {
        var path = Assert.Single(provider.Files.Keys.Distinct(StringComparer.OrdinalIgnoreCase),
            candidate => candidate.EndsWith(packageSuffix, StringComparison.OrdinalIgnoreCase));
        AssertSelectedArchive(provider.Files[path], expectedArchive);
    }

    private static void AssertSelectedArchive(GameFile file, string expectedArchive)
    {
        var entry = Assert.IsAssignableFrom<VfsEntry>(file);
        Assert.Equal(expectedArchive, entry.Vfs.Name);
        Assert.True(entry.Vfs.ReadOrder >= 100,
            $"Expected a patch read order, got {entry.Vfs.ReadOrder} for {entry.Vfs.Name}.");
    }
}
