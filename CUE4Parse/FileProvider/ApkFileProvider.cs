using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Ionic.Zip;

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
        var zipFile = new ZipFile(_apkFile.FullName);
        foreach (var entry in zipFile.SelectEntries("*main.obb.png"))
        {
            MemoryStream pngStream = new();
            entry.Extract(pngStream);
            pngStream.Seek(0, SeekOrigin.Begin);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var container = ZipFile.Read(pngStream);

            foreach (var fileEntry in container.Entries)
            {
                var streams = new Stream[2];
                streams[0] = new MemoryStream();
                fileEntry.Extract(streams[0]);
                streams[0].Position = 0;

                var upperExt = fileEntry.FileName.SubstringAfterLast('.').ToUpper();
                switch (upperExt)
                {
                    case "PAK":
                        RegisterVfs(fileEntry.FileName, streams);
                        continue;
                    case "UTOC":
                        if (container.SelectEntries($"{fileEntry.FileName.SubstringBeforeLast('.') + ".ucas"}").First() is { } ucasEntry)
                        {
                            streams[1] = new MemoryStream();
                            ucasEntry.Extract(streams[1]);
                            streams[1].Position = 0;
                        }
                        RegisterVfs(fileEntry.FileName, streams);
                        continue;
                }

                // Register local file only if it has a known extension, we don't need every file
                if (!GameFile.Ue4KnownExtensions.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                    continue;

                var osFile = new StreamedGameFile(fileEntry.FileName, streams[0], Versions);
                if (IsCaseInsensitive) osFiles[osFile.Path.ToLowerInvariant()] = osFile;
                else osFiles[osFile.Path] = osFile;
            }
        }

        _files.AddFiles(osFiles);
    }
}
