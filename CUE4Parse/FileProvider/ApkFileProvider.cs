using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider;

public class ApkFileProvider : DefaultFileProvider
{
    private readonly FileInfo _apkFile;

    public ApkFileProvider(string file, bool isCaseInsensitive = false, VersionContainer? versions = null)
        : this(new FileInfo(file), isCaseInsensitive, versions) { }
    public ApkFileProvider(FileInfo apkFile, bool isCaseInsensitive = false, VersionContainer? versions = null)
        : base(apkFile.Directory ?? new DirectoryInfo(""), SearchOption.TopDirectoryOnly, isCaseInsensitive, versions)
    {
        _apkFile = apkFile;
    }

    public override void Initialize()
    {
        if (!_apkFile.Exists)
            throw new FileNotFoundException("Given APK file must exist");

        var osFiles = new Dictionary<string, GameFile>();
        using var apkFs = File.OpenRead(_apkFile.FullName);
        using var zipFile = new ZipArchive(apkFs, ZipArchiveMode.Read);
        foreach (var pngEntry in zipFile.Entries.Where(x => x.FullName.EndsWith("main.obb.png", StringComparison.OrdinalIgnoreCase)))
        {
            var pngStream = new MemoryStream((int)pngEntry.Length);
            {
                using var pngEntryStream = pngEntry.Open();
                pngEntryStream.CopyTo(pngStream);
            }
            pngStream.Position = 0;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var container = new ZipArchive(pngStream, ZipArchiveMode.Read);

            foreach (var fileEntry in container.Entries)
            {
                var streams = new Stream[2];
                streams[0] = new MemoryStream((int)fileEntry.Length);
                {
                    using var fileEntryStream = fileEntry.Open();
                    fileEntryStream.CopyTo(streams[0]);
                }
                streams[0].Position = 0;

                var upperExt = fileEntry.Name.SubstringAfterLast('.').ToUpperInvariant();
                switch (upperExt)
                {
                    case "PAK":
                        RegisterVfs(fileEntry.Name, streams);
                        continue;
                    case "UTOC":
                        if (container.Entries.FirstOrDefault(x => x.Name == $"{fileEntry.Name.SubstringBeforeLast('.')}.ucas") is { } ucasEntry)
                        {
                            streams[1] = new MemoryStream((int)ucasEntry.Length);
                            {
                                using var ucasEntryStream = ucasEntry.Open();
                                ucasEntryStream.CopyTo(streams[1]);
                            }
                            streams[1].Position = 0;
                        }
                        RegisterVfs(fileEntry.Name, streams);
                        continue;
                }

                // Register local file only if it has a known extension, we don't need every file
                if (!GameFile.Ue4KnownExtensions.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                    continue;

                var osFile = new StreamedGameFile(fileEntry.Name, streams[0], Versions);
                if (IsCaseInsensitive) osFiles[osFile.Path.ToLowerInvariant()] = osFile;
                else osFiles[osFile.Path] = osFile;
            }
        }

        _files.AddFiles(osFiles);
    }
}
