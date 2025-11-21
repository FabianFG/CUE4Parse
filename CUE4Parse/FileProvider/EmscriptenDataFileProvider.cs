using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider;

public class EmscriptenPackMetadata
{
    public List<EmscriptenFileEntry> Files { get; set; } = [];
}

public class EmscriptenFileEntry
{
    public string Filename { get; set; } = "";
    public long Start { get; set; }
    public long End { get; set; }
}

public class EmscriptenDataFileProvider(
    FileInfo dataFile,
    FileInfo? metadataFile = null,
    VersionContainer? versions = null,
    StringComparer? pathComparer = null)
    : DefaultFileProvider(dataFile.Directory ?? new DirectoryInfo(""), SearchOption.TopDirectoryOnly, versions, pathComparer)
{
    private readonly FileInfo _metadataFile = metadataFile ?? new FileInfo(dataFile.FullName.Replace(dataFile.Name, "metadata.json"));

    public EmscriptenDataFileProvider(
        string dataFilePath,
        string? metadataFilePath = null,
        VersionContainer? versions = null,
        StringComparer? pathComparer = null)
        : this(new FileInfo(dataFilePath), metadataFilePath != null ? new FileInfo(metadataFilePath) : null, versions, pathComparer) { }

    public override void Initialize()
    {
        if (!dataFile.Exists)
            throw new FileNotFoundException("Given Emscripten data file must exist", dataFile.FullName);
        if (!_metadataFile.Exists)
            throw new FileNotFoundException("Given metadata file must exist", _metadataFile.FullName);

        var json = File.ReadAllText(_metadataFile.FullName);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var metadata = JsonSerializer.Deserialize<EmscriptenPackMetadata>(json, options)
                       ?? throw new InvalidDataException("Invalid metadata JSON");

        if (metadata.Files is null || metadata.Files.Count == 0)
            throw new InvalidDataException("Invalid metadata: files array missing or empty");

        var entries = new Dictionary<string, EmscriptenFileEntry>(PathComparer);
        foreach (var f in metadata.Files)
        {
            if (string.IsNullOrEmpty(f.Filename))
                throw new InvalidDataException("Invalid file entry: filename missing");
            if (f.Start < 0 || f.End < f.Start)
                throw new InvalidDataException($"Invalid file entry offsets for '{f.Filename}'");
            var name = NormalizePath(f.Filename);
            entries[name] = f;
        }

        var osFiles = new Dictionary<string, GameFile>(PathComparer);

        foreach (var kv in entries)
        {
            var name = kv.Key;
            var entry = kv.Value;
            var upperExt = name.SubstringAfterLast('.').ToUpperInvariant();

            switch (upperExt)
            {
                case "PAK":
                {
                    var streams = new Stream[2];
                    streams[0] = Slice(entry);
                    RegisterVfs(name, streams);
                    continue;
                }
                case "UTOC":
                {
                    var baseName = name.SubstringBeforeLast('.');
                    var ucasName = $"{baseName}.ucas";
                    var streams = new Stream[2];
                    streams[0] = Slice(entry);

                    if (entries.TryGetValue(ucasName, out var ucasEntry))
                    {
                        streams[1] = Slice(ucasEntry);
                    }

                    RegisterVfs(name, streams);
                    continue;
                }
            }

            // Register local file only if it has a known extension
            if (!GameFile.UeKnownExtensions.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                continue;

            var osFile = new StreamedGameFile(name, Slice(entry), Versions);
            osFiles[osFile.Path] = osFile;
        }

        Files.AddFiles(osFiles);
    }

    private static string NormalizePath(string filename)
    {
        var name = filename.TrimStart('/').Replace('\\', '/');
        return name;
    }

    private SegmentFileStream Slice(EmscriptenFileEntry fe) => new(dataFile.FullName, fe.Start, fe.End - fe.Start);
}