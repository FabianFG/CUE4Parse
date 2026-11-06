using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.MarchOfGiants.Vfs;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.MarchOfGiants.FileProvider;

public class MogFileProvider(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly, VersionContainer? versions = null, StringComparer? pathComparer = null)
    : DefaultFileProvider(directory, searchOption, versions, pathComparer)
{
    private static readonly string _mogExtension = ".mog";

    public override void Initialize()
    {
        base.Initialize();
        foreach (var file in _workingDirectory.EnumerateFiles("*", _searchOption))
        {
            if (file.Extension.Equals(_mogExtension, StringComparison.OrdinalIgnoreCase))
            {
                RegisterVfs(file);
            }
        }
    }

    public override void RegisterVfs(string file)
    {
        if (!Path.GetExtension(file).Equals(_mogExtension, StringComparison.OrdinalIgnoreCase))
        {
            base.RegisterVfs(file);
            return;
        }

        var archive = new FRandomAccessFileStreamArchive(file, Versions);
        try
        {
            PostLoadReader(new MogFileReader(archive));
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }
}
