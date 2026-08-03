using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider.Objects;

public class OsGameFile : VersionedGameFile
{
    public readonly FileInfo ActualFile;

    public OsGameFile(FileInfo info, VersionContainer versions) : base(info.FullName.Replace('\\', '/'), info.Length, versions)
    {
        ActualFile = info;
    }

    public OsGameFile(DirectoryInfo baseDir, FileInfo info, string mountPoint, VersionContainer versions)
        : base(GetPath(baseDir, info, mountPoint), info.Length, versions)
    {
        ActualFile = info;
    }

    // In case loose files are in %localappdata% we need to sanitize the path so it won't start from the root
    private static readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Replace('\\', '/').TrimEnd('/');
    private static string GetPath(DirectoryInfo baseDir, FileInfo info, string mountPoint)
    {
        var path = System.IO.Path.GetRelativePath(baseDir.FullName, info.FullName).Replace('\\', '/');
        if (path.StartsWith(_localAppData, StringComparison.OrdinalIgnoreCase))
            path = path[_localAppData.Length..].TrimStart('/');

        return mountPoint + path;
    }

    public override bool IsEncrypted => false;
    public override CompressionMethod CompressionMethod => CompressionMethod.None;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Read(FByteBulkDataHeader? header = null)
    {
        if (header != null)
        {
            using var stream = ActualFile.OpenRead();
            stream.Seek(header.Value.OffsetInFile, SeekOrigin.Begin);
            var buffer = new byte[header.Value.SizeOnDisk];
            stream.ReadExactly(buffer, 0, buffer.Length);
            return buffer;
        }

        return File.ReadAllBytes(ActualFile.FullName);
    }
}
