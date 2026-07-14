using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public class DefaultFileProvider : AbstractVfsFileProvider
    {
        protected readonly DirectoryInfo _workingDirectory;
        protected readonly DirectoryInfo[] _extraDirectories;
        protected readonly SearchOption _searchOption;

        private readonly record struct LooseFileDiscovery(Dictionary<string, GameFile> Files, int FilesCount);

        [Obsolete("Use the other constructors with explicit StringComparer")]
        public DefaultFileProvider(string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(new DirectoryInfo(directory), searchOption, isCaseInsensitive, versions) { }
        [Obsolete("Use the other constructors with explicit StringComparer")]
        public DefaultFileProvider(DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(directory, [], searchOption, isCaseInsensitive, versions) { }
        [Obsolete("Use the other constructors with explicit StringComparer")]
        public DefaultFileProvider(DirectoryInfo directory, DirectoryInfo[] extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
            : this(directory, extraDirectories, searchOption, versions, isCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public DefaultFileProvider(
            string directory,
            SearchOption searchOption,
            VersionContainer? versions = null,
            StringComparer? pathComparer = null)
            : this(new DirectoryInfo(directory), searchOption, versions, pathComparer) { }
        public DefaultFileProvider(
            DirectoryInfo directory,
            SearchOption searchOption,
            VersionContainer? versions = null,
            StringComparer? pathComparer = null)
            : this(directory, [], searchOption, versions, pathComparer) { }
        public DefaultFileProvider(
            DirectoryInfo directory,
            DirectoryInfo[] extraDirectories,
            SearchOption searchOption,
            VersionContainer? versions = null,
            StringComparer? pathComparer = null)
            : base(versions, pathComparer)
        {
            _workingDirectory = directory;
            _extraDirectories = extraDirectories;
            _searchOption = searchOption;
        }

        public override void Initialize()
        {
            if (!_workingDirectory.Exists)
                throw new DirectoryNotFoundException("The game directory could not be found.");

            var availableFiles = new List<LooseFileDiscovery> {IterateFiles(_workingDirectory, _searchOption)};
            if (_extraDirectories is {Length: > 0})
            {
                availableFiles.AddRange(_extraDirectories.Select(directory => IterateFiles(directory, _searchOption)));
            }

            foreach (var osFiles in availableFiles)
            {
                Files.AddFiles(osFiles.Files);
                LooseFileCount += osFiles.FilesCount;
            }
        }

        private LooseFileDiscovery IterateFiles(DirectoryInfo directory, SearchOption option)
        {
            var packageCount = 0;
            var osFiles = new Dictionary<string, GameFile>(PathComparer);

            if (!directory.Exists) return new LooseFileDiscovery(osFiles, packageCount);

            // Look for .uproject file to get the correct mount point
            var uproject = directory.GetFiles("*.uproject", SearchOption.TopDirectoryOnly).FirstOrDefault();
            string mountPoint;
            if (uproject != null)
            {
                mountPoint = uproject.Name.SubstringBeforeLast('.') + '/';
            }
            else
            {
                // Or use the directory name
                mountPoint = directory.Name + '/';
            }

            // In .uproject mode, we must recursively look for files
            option = uproject != null ? SearchOption.AllDirectories : option;

            foreach (var file in directory.EnumerateFiles("*.*", option))
            {
                var upperExt = file.Extension.SubstringAfter('.').ToUpper();

                // Only load containers if .uproject file is not found
                if (uproject is null && (upperExt is "PAK" or "UTOC" || (upperExt == "UPAK" && Versions.Game is GAME_LordOfMysteries)))
                {
                    if (file.FullName.Contains(@"Binaries\ThirdParty\CEF", StringComparison.OrdinalIgnoreCase) || file.FullName.Contains(@"Binaries\Win32\host") || file.FullName.Contains(@"Binaries\Win64\host") || file.FullName.Contains(@"\qtwebengine_") || file.FullName.Contains(@"NexonPlatformWebView\ThirdParty") || file.FullName.Contains("SnapversePCGameSDK")) continue;
                    RegisterVfs(file);
                    continue;
                }

                if (uproject is null && OnDemandOptions is not null && upperExt is "UONDEMANDTOC")
                {
                    var ioChunkTok = new IoChunkToc(file.FullName, Versions);
                    RegisterVfs(ioChunkTok);
                    continue;
                }

                if (upperExt is "TFC")
                {
                    RegisterTextureCache(file);
                    continue;
                }

                // Register local file only if it has a known extension, we don't need every file
                if (!GameFile.UeKnownExtensions.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                    continue;
                if (!GameFile.UePackagePayloadExtensionsSet.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                    packageCount++;

                var osFile = new OsGameFile(_workingDirectory, file, mountPoint, Versions);
                osFiles[osFile.Path] = osFile;
            }

            return new LooseFileDiscovery(osFiles, packageCount);
        }
    }
}
