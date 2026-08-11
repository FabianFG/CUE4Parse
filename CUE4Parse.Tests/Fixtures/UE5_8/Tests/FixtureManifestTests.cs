using System.Text.Json;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureManifestTests
{
    [Fact]
    public void ManifestCoversPersistentFixtureFiles()
    {
        var fixtureRoot = Path.GetFullPath(FixturePath());
        var manifestPath = Path.Combine(fixtureRoot, "manifest.json");
        Assert.True(File.Exists(manifestPath), $"Missing fixture manifest: {manifestPath}");

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var files = manifest.RootElement.GetProperty("files").EnumerateArray().ToArray();
        var declaredPaths = new List<string>(files.Length);

        foreach (var entry in files)
        {
            var relativePath = entry.GetProperty("path").GetString();
            Assert.False(string.IsNullOrWhiteSpace(relativePath));
            declaredPaths.Add(relativePath!.Replace('/', Path.DirectorySeparatorChar));

            var path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath));
            Assert.StartsWith(fixtureRoot + Path.DirectorySeparatorChar, path, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(path), $"Missing persistent fixture: {relativePath}");
        }

        var actualPaths = Directory.EnumerateFiles(fixtureRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(fixtureRoot, path))
            .Where(path => !path.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) &&
                           !path.Equals("README.md", StringComparison.OrdinalIgnoreCase) &&
                           !path.StartsWith($"Tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(actualPaths, declaredPaths.Order(StringComparer.Ordinal).ToArray());
    }
}
