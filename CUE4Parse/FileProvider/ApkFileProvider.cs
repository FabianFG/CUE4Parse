using System.IO.Compression;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider;

public class ApkFileProvider : DefaultFileProvider
{
    private readonly FileInfo _apkFile;

    [Obsolete("Use the other constructors with explicit StringComparer")]
    public ApkFileProvider(string file, bool isCaseInsensitive = false, VersionContainer? versions = null)
        : this(new FileInfo(file), isCaseInsensitive, versions) { }
    [Obsolete("Use the other constructors with explicit StringComparer")]
    public ApkFileProvider(FileInfo apkFile, bool isCaseInsensitive = false, VersionContainer? versions = null)
        : this(apkFile, versions, isCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

    public ApkFileProvider(
        string file,
        VersionContainer? versions = null,
        StringComparer? pathComparer = null)
        : this(new FileInfo(file), versions, pathComparer) { }
    public ApkFileProvider(
        FileInfo apkFile,
        VersionContainer? versions = null,
        StringComparer? pathComparer = null)
        : base(apkFile.Directory ?? new DirectoryInfo(""), SearchOption.TopDirectoryOnly, versions, pathComparer)
    {
        _apkFile = apkFile;
    }

    public override void Initialize()
    {
        var osFiles = new Dictionary<string, GameFile>(PathComparer);
        LooseFileCount += LoadInto(_apkFile, this, osFiles);
        Files.AddFiles(osFiles);
    }

    internal static int LoadInto(FileInfo apkFile, DefaultFileProvider provider, Dictionary<string, GameFile> osFiles)
    {
        if (!apkFile.Exists)
            throw new FileNotFoundException("Given APK file must exist", apkFile.FullName);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var apkStream = File.OpenRead(apkFile.FullName);
        using var apk = new ZipArchive(apkStream, ZipArchiveMode.Read);

        return LoadArchive(apk, provider, osFiles);
    }

    private static int LoadArchive(ZipArchive apk, DefaultFileProvider provider, Dictionary<string, GameFile> osFiles)
    {
        var packageCount = 0;
        foreach (var nestedApkEntry in apk.Entries.Where(x => x.FullName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)))
        {
            using var nestedApkStream = CopyEntry(nestedApkEntry);
            using var nestedApk = new ZipArchive(nestedApkStream, ZipArchiveMode.Read);
            packageCount += LoadArchive(nestedApk, provider, osFiles);
        }

        foreach (var obbEntry in apk.Entries.Where(x => x.FullName.EndsWith("main.obb.png", StringComparison.OrdinalIgnoreCase) || x.FullName.EndsWith(".obb", StringComparison.OrdinalIgnoreCase)))
        {
            using var obbStream = CopyEntry(obbEntry);
            using var obb = new ZipArchive(obbStream, ZipArchiveMode.Read);
            packageCount += LoadEntries(obb, provider, osFiles);
        }

        packageCount += LoadEntries(apk, provider, osFiles);
        return packageCount;
    }

    private static int LoadEntries(ZipArchive archive, DefaultFileProvider provider, Dictionary<string, GameFile> osFiles)
    {
        var packageCount = 0;

        foreach (var fileEntry in archive.Entries)
        {
            var filePath = fileEntry.FullName.NormalizePath();
            var upperExt = filePath.SubstringAfterLast('.').ToUpperInvariant();
            switch (upperExt)
            {
                case "PAK":
                case "UPAK" when provider.Versions.Game is GAME_LordOfMysteries:
                    provider.RegisterVfs(filePath, [CopyEntry(fileEntry)]);
                    continue;
                case "UTOC":
                {
                    var streams = new Stream[2];
                    streams[0] = CopyEntry(fileEntry);
                    var ucasPath = $"{filePath.SubstringBeforeLast('.')}.ucas";
                    if (archive.Entries.FirstOrDefault(x => x.FullName.NormalizePath().Equals(ucasPath, provider.StringComparison)) is { } ucasEntry)
                    {
                        streams[1] = CopyEntry(ucasEntry);
                    }
                    provider.RegisterVfs(filePath, streams);
                    continue;
                }
            }

            // Register local file only if it has a known extension, we don't need every file
            if (!GameFile.UeKnownExtensionsSet.Contains(upperExt))
                continue;
            if (!GameFile.UePackagePayloadExtensionsSet.Contains(upperExt))
                packageCount++;

            var osFile = new StreamedGameFile(filePath, CopyEntry(fileEntry), provider.Versions);
            osFiles[osFile.Path] = osFile;
        }

        return packageCount;
    }

    private static MemoryStream CopyEntry(ZipArchiveEntry entry)
    {
        var stream = new MemoryStream((int) entry.Length);
        using var entryStream = entry.Open();
        entryStream.CopyTo(stream);
        stream.Position = 0;
        return stream;
    }
}
