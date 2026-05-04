using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.HonorOfKings.Vfs;
using CUE4Parse.GameTypes.HonorOfKings.Vfs.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.HonorOfKings.FileProvider;

public class HoKWDefaultFileProvider : DefaultFileProvider
{
    public static string GeneratedIndexFolder;

    public HoKWDefaultFileProvider(string directory, SearchOption searchOption, VersionContainer? versions = null,
        StringComparer? pathComparer = null) : base(directory, searchOption, versions, pathComparer)
    {
    }

    public override void Initialize()
    {
        if (!_workingDirectory.Exists)
            throw new DirectoryNotFoundException("The game directory could not be found.");

        var index = _workingDirectory.EnumerateFiles("1.db", _searchOption).FirstOrDefault();
        if (index is null)
        {
            Log.Warning("Can't find 1.db for building an index.");
            var generatedIndex = _workingDirectory.EnumerateDirectories("GeneratedIndex", _searchOption).FirstOrDefault();
            GeneratedIndexFolder = Path.GetFullPath(generatedIndex is null ? Path.Combine(_workingDirectory.FullName, "GeneratedIndex") : generatedIndex.FullName);
        }
        else
        {
            GeneratedIndexFolder = Path.GetFullPath(Path.Combine(index.DirectoryName!, "..", "GeneratedIndex"));
        }

        if (!Directory.Exists(GeneratedIndexFolder))
        {
            Directory.CreateDirectory(GeneratedIndexFolder);
        }

        Task.WhenAll(
            BuildFileIndex(index),
            Task.Run(() => IterateFiles(_workingDirectory, _searchOption))
        ).GetAwaiter().GetResult();
    }

    private bool RegisterDbVfs(FileInfo file, [NotNullWhen(true)] out HoKdbFileReader? reader)
    {
        reader = null;
        try
        {
            reader = new HoKdbFileReader(file, this, Versions);
            return true;
        }
        catch (Exception)
        {
            Log.Error("Failed to open {0}", file.FullName);
        }

        return false;
    }

    private void IterateFiles(DirectoryInfo directory, SearchOption option)
    {
        foreach (var files in directory.EnumerateFiles("*.db", option).GroupBy(x => x.DirectoryName))
        {
            var dir = files.Key;
            var file = Path.Combine(dir, dir.SubstringAfterLast('\\') + ".db");
            if (File.Exists(file) && RegisterDbVfs(new FileInfo(file), out var reader))
            {
                PostLoadReader(reader);
            }
        }
    }

    private async Task BuildFileIndex(FileInfo? mainIndex)
    {
        var indexFile = await ReadGeneratedFileIndex(GeneratedIndexFolder).ConfigureAwait(false);
        if (mainIndex is null) return;

        try
        {
            var reader = new HoKdbFileReader(mainIndex, null, Versions);
            await reader.BuildIndex(GeneratedIndexFolder, indexFile).ConfigureAwait(false);
        }
        catch (Exception)
        {
            Log.Error("Failed to build index for {0}", mainIndex.FullName);
        }

        Log.Information("Regenerated file index.");
    }

    private static async Task<string> ReadGeneratedFileIndex(string indexDirectory)
    {
        // maybe binary with hashes would be better
        var path = Path.Combine(indexDirectory, "FileIndex.ind");
        if (!File.Exists(path)) return path;

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 64,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (!line.StartsWith('/'))
            {
                HoKdbFileReader.HashMap[FHoKFileHash.Compute(line, true)] = line;
            }
            else
            {
                HoKdbFileReader.HashMap[FHoKFileHash.Compute(line, false)] = line[1..];
            }
        }

        return path;
    }
}
