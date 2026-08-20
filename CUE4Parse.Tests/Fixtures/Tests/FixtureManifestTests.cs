using System.Text.Json;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureManifestTests
{
    [Fact]
    public void ManifestCoversPersistentContainerInventory()
    {
        var fixtureRoot = Path.GetFullPath(FixturePath());
        var manifestPath = Path.Combine(fixtureRoot, "manifest.json");
        Assert.True(File.Exists(manifestPath), $"Missing fixture manifest: {manifestPath}");

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;
        var engine = root.GetProperty("engine");
        Assert.Equal(FixtureEngineVersion,
            (engine.GetProperty("major").GetInt32(), engine.GetProperty("minor").GetInt32()));
        var declaredPaths = new List<string>();

        foreach (var variant in root.GetProperty("variants").EnumerateObject())
        {
            AddEntries(variant.Value.GetProperty("containers"),
                Path.Combine("IoStore", variant.Name));
        }
        AddEntries(root.GetProperty("pakFixtures"), "Pak");
        AddEntries(root.GetProperty("legacyPakFixtures"), "LegacyPak");
        AddEntries(root.GetProperty("androidTexturePakFixtures"), "AndroidPak");
        AddEntries(root.GetProperty("patchFixtures"), "Patch");

        var actualPaths = new[] { "IoStore", "Pak", "LegacyPak", "AndroidPak", "Patch" }
            .SelectMany(directory => Directory.EnumerateFiles(
                Path.Combine(fixtureRoot, directory), "*", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(fixtureRoot, path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(actualPaths, declaredPaths.Order(StringComparer.Ordinal).ToArray());

        return;

        void AddEntries(JsonElement entries, string directory)
        {
            foreach (var entry in entries.EnumerateArray())
            {
                var name = entry.GetProperty("name").GetString();
                Assert.False(string.IsNullOrWhiteSpace(name));
                var relativePath = Path.Combine(directory,
                    name!.Replace('/', Path.DirectorySeparatorChar));
                declaredPaths.Add(relativePath);

                var path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath));
                Assert.StartsWith(fixtureRoot + Path.DirectorySeparatorChar, path,
                    StringComparison.OrdinalIgnoreCase);
                Assert.True(File.Exists(path), $"Missing persistent fixture: {relativePath}");
            }
        }
    }
}
