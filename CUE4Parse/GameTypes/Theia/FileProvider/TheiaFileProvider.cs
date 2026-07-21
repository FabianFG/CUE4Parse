using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Theia.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.Theia.FileProvider;

public class TheiaFileProvider : DefaultFileProvider
{
    public TheiaFileProvider(string directory, SearchOption searchOption, VersionContainer? versions = null, StringComparer? pathComparer = null)
        : this(new DirectoryInfo(directory), searchOption, versions, pathComparer) { }

    public TheiaFileProvider(DirectoryInfo directory, SearchOption searchOption, VersionContainer? versions = null, StringComparer? pathComparer = null)
        : this(directory, [], searchOption, versions, pathComparer) { }

    public TheiaFileProvider(DirectoryInfo directory, DirectoryInfo[] extraDirectories, SearchOption searchOption, VersionContainer? versions = null, StringComparer? pathComparer = null)
        : base(directory, extraDirectories, searchOption, versions, pathComparer) { }

    private FArchive OpenContainerArchive(string path) => File.Exists(path + ".meta") ? new FTheiaArchive(path, Versions) : new FRandomAccessFileStreamArchive(path, Versions);

    public override void RegisterVfs(FileInfo file) => RegisterVfs(file.FullName);
    public override void RegisterVfs(string file) => RegisterVfs(OpenContainerArchive(file), null, OpenContainerArchive);
}
