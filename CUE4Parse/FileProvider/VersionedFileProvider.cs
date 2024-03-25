#nullable disable
using System;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider;

public class VersionedFileProvider : DefaultFileProvider
{
    private FileProviderDictionary _originals;
    private ILookup<string, VersionedGameFile> _versionedFilesLookup;
    private StringComparer _comparer;

    public VersionedFileProvider(string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer versions = null) : base(
        directory, searchOption, isCaseInsensitive, versions)
    {
    }

    public VersionedFileProvider(DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer versions = null) : base(
        directory, searchOption, isCaseInsensitive, versions)
    {
    }

    public VersionedFileProvider(DirectoryInfo directory, DirectoryInfo[] extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false,
        VersionContainer versions = null) : base(directory, extraDirectories, searchOption, isCaseInsensitive, versions)
    {
    }

    public void SetToAll()
    {
        Init();
        _files.Clear();
        _files.AddFiles(_originals);
    }

    public void SetToLatestFiles()
    {
        Init();

        var dict = _versionedFilesLookup.ToDictionary(f => f.Key, f => f.MaxBy(vf => vf.Version).GameFile, _comparer);
        _files.Clear();
        _files.AddFiles(dict);
    }

    public void SetToVersion(int version)
    {
        Init();
        var dict = _versionedFilesLookup.Where(f => f.Any(vf => vf.Version <= version))
            .ToDictionary(f => f.Key, f => f.Where(vf => vf.Version <= version).MaxBy(vf => vf.Version).GameFile, _comparer);
        _files.Clear();
        _files.AddFiles(dict);
    }


    private void Init()
    {
        if (_originals == null)
        {
            _originals = new FileProviderDictionary(IsCaseInsensitive);
            _originals.AddFiles(_files);
        }

        _comparer = IsCaseInsensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        _versionedFilesLookup ??= _originals.ToLookup(f => f.Key, f => new VersionedGameFile(f.Value), _comparer);
    }

    private class VersionedGameFile(GameFile gameFile)
    {
        public GameFile GameFile { get; } = gameFile;
        public int Version { get; } = GetVersionFromFile(gameFile);


        private static int GetVersionFromFile(GameFile gameFile)
        {
            var packageFileName = gameFile switch
            {
                FPakEntry file => Path.GetFileNameWithoutExtension(file.PakFileReader.Name),
                FIoStoreEntry floFile => Path.GetFileNameWithoutExtension(floFile.IoStoreReader.Name),
                _ => throw new Exception("Unknown file type")
            };

            var entries = packageFileName.Split('_');
            if (entries.Length < 3)
                return -1;
            if (entries[^1] != "P")
                return -1;
            if (!int.TryParse(entries[^2], out var version))
                return -1;
            return version;
        }
    }
}