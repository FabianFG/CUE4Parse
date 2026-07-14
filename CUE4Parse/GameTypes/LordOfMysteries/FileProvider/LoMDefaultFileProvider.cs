using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.LordOfMysteries.Vfs;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.LordOfMysteries.FileProvider;

public class LoMDefaultFileProvider(string directory, SearchOption searchOption, VersionContainer? versions = null, StringComparer? pathComparer = null) : DefaultFileProvider(directory, searchOption, versions, pathComparer)
{
    public override void Initialize()
    {
        base.Initialize();

        if (!_workingDirectory.Exists)
            throw new DirectoryNotFoundException("The game directory could not be found.");

        var directory = LoMDirectoryIndex.Read(_workingDirectory);
        var manifest = _workingDirectory.EnumerateFiles("package.manifest", _searchOption).FirstOrDefault();
        if (manifest == null)
        {
            Log.Error("Failed to find Lord of Mysteries manifest");
            return;
        }

        IReadOnlyList<LoMIoStoreManifest> containers;
        try
        {
            containers = LoMIoStoreManifest.Read(manifest, Versions);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open Lord of Mysteries manifest {Manifest}", manifest.FullName);
            return;
        }

        foreach (var container in containers)
        {
            try
            {
                PostLoadReader(new LoMIoStoreReader(container, directory, Versions));
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load container {Container}", container.TocArchive.Name);
            }
        }
    }
}
