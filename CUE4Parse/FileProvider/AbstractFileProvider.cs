using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Plugins;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.FileProvider
{
    public abstract class AbstractFileProvider : IFileProvider
    {
        protected static readonly ILogger Log = Serilog.Log.ForContext<IFileProvider>();

        public virtual VersionContainer Versions { get; set; }
        public virtual ITypeMappingsProvider? MappingsContainer { get; set; }
        public virtual TypeMappings? MappingsForThisGame => MappingsContainer?.ForGame(GameName.ToLowerInvariant());
        public virtual IDictionary<string, IDictionary<string, string>> LocalizedResources { get; } = new Dictionary<string, IDictionary<string, string>>();
        public Dictionary<string, string> VirtualPaths { get; } = new();
        public abstract IReadOnlyDictionary<string, GameFile> Files { get; }
        public abstract IReadOnlyDictionary<FPackageId, GameFile> FilesById { get; }
        public virtual bool IsCaseInsensitive { get; } // fabian? is this reversed?
        public bool ReadScriptData { get; set; } = false;
        public virtual bool UseLazySerialization { get; set; } = true;

        protected AbstractFileProvider(bool isCaseInsensitive = false, VersionContainer? versions = null)
        {
            IsCaseInsensitive = isCaseInsensitive;
            Versions = versions ?? VersionContainer.DEFAULT_VERSION_CONTAINER;
        }

        private string _gameName;
        public virtual string GameName
        {
            get
            {
                if (string.IsNullOrEmpty(_gameName))
                {
                    string t = Files.Keys.FirstOrDefault(it => !it.SubstringBefore('/').EndsWith("engine", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                    _gameName = t.SubstringBefore('/');
                }
                return _gameName;
            }
        }

        public virtual int LoadLocalization(ELanguage language = ELanguage.English, CancellationToken cancellationToken = default)
        {
            var regex = new Regex($"^{GameName}/.+/{GetLanguageCode(language)}/.+.locres$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            LocalizedResources.Clear();

            var i = 0;
            foreach (var file in Files.Where(x => regex.IsMatch(x.Key)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!file.Value.TryCreateReader(out var archive)) continue;

                var locres = new FTextLocalizationResource(archive);
                foreach (var entries in locres.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!LocalizedResources.ContainsKey(entries.Key.Str))
                        LocalizedResources[entries.Key.Str] = new Dictionary<string, string>();

                    foreach (var keyValue in entries.Value)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        LocalizedResources[entries.Key.Str][keyValue.Key.Str] = keyValue.Value.LocalizedString;
                        i++;
                    }
                }
            }
            return i;
        }

        public virtual string GetLocalizedString(string namespacee, string key, string? defaultValue)
        {
            if (LocalizedResources.TryGetValue(namespacee, out var keyValue) &&
                keyValue.TryGetValue(key, out var localizedResource))
                return localizedResource;

            return defaultValue ?? string.Empty;
        }

        private string GetLanguageCode(ELanguage language)
        {
            return GameName.ToLowerInvariant() switch
            {
                "fortnitegame" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.SpanishLatin => "es-419",
                    ELanguage.Arabic => "ar",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.Polish => "pl",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Turkish => "tr",
                    ELanguage.Chinese => "zh-CN",
                    ELanguage.TraditionalChinese => "zh-Hant",
                    _ => "en"
                },
                "worldexplorers" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Chinese => "zh-Hans",
                    _ => "en"
                },
                "shootergame" => language switch
                {
                    ELanguage.English => "en-US",
                    ELanguage.French => "fr-FR",
                    ELanguage.German => "de-DE",
                    ELanguage.Italian => "it-IT",
                    ELanguage.Spanish => "es-ES",
                    ELanguage.SpanishMexico => "es-MX",
                    ELanguage.Arabic => "ar-AE",
                    ELanguage.Japanese => "ja-JP",
                    ELanguage.Korean => "ko-KR",
                    ELanguage.Polish => "pl-PL",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru-RU",
                    ELanguage.Turkish => "tr-TR",
                    ELanguage.Chinese => "zh-CN",
                    ELanguage.TraditionalChinese => "zh-TW",
                    ELanguage.Indonesian => "id-ID",
                    ELanguage.Thai => "th-TH",
                    ELanguage.VietnameseVietnam => "vi-VN",
                    _ => "en-US"
                },
                "stateofdecay2" => language switch
                {
                    ELanguage.English => "en-US",
                    ELanguage.AustralianEnglish => "en-AU",
                    ELanguage.French => "fr-FR",
                    ELanguage.German => "de-DE",
                    ELanguage.Italian => "it-IT",
                    ELanguage.SpanishMexico => "es-MX",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru-RU",
                    ELanguage.Chinese => "zh-CN",
                    _ => "en-US"
                },
                "oakgame" => language switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Chinese => "zh-Hans-CN",
                    ELanguage.TraditionalChinese => "zh-Hant-TW",
                    _ => "en"
                },
                _ => "en"
            };
        }

        public virtual int LoadVirtualPaths(CancellationToken cancellationToken = default)
        {
            var regex = new Regex($"^{GameName}/Plugins/.+.upluginmanifest$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            VirtualPaths.Clear();

            var i = 0;
            foreach (var file in Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!regex.IsMatch(file.Key)) continue;
                if (!TryCreateReader(file.Value.Path, out var stream)) continue;
                using var reader = new StreamReader(stream);
                var manifest = JsonConvert.DeserializeObject<UPluginManifest>(reader.ReadToEnd());

                foreach (var content in manifest.Contents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!content.Descriptor.CanContainContent) continue;
                    var virtPath = content.File.SubstringAfterLast('/').SubstringBeforeLast('.');
                    var path = content.File.Replace("../../../", string.Empty).SubstringBeforeLast('/');

                    if (!VirtualPaths.ContainsKey(virtPath))
                    {
                        VirtualPaths.Add(virtPath, path);
                        i++; // Only increment if we don't have the path already
                    }
                    else
                    {
                        VirtualPaths[virtPath] = path;
                    }
                }
            }

            return i;
        }
        
        public virtual GameFile this[string path] => Files[FixPath(path)];

        public virtual bool TryFindGameFile(string path, out GameFile file)
        {
            var uassetPath = FixPath(path);
            if (Files.TryGetValue(uassetPath, out file))
            {
                return true;
            }

            var umapPath = uassetPath.SubstringBeforeWithLast('.') + GameFile.Ue4PackageExtensions[1];
            if (Files.TryGetValue(umapPath, out file))
            {
                return true;
            }

            return Files.TryGetValue(IsCaseInsensitive ? path.ToLowerInvariant() : path, out file);
        }

        public virtual string FixPath(string path) => FixPath(path, IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        public virtual string FixPath(string path, StringComparison comparisonType)
        {
            path = path.Replace('\\', '/');
            if (path[0] == '/')
                path = path[1..];
            var lastPart = path.SubstringAfterLast('/');
            // This part is only for FSoftObjectPaths and not really needed anymore internally, but it's still in here for user input
            if (lastPart.Contains('.') && lastPart.SubstringBefore('.') == lastPart.SubstringAfter('.'))
                path = string.Concat(path.SubstringBeforeLast('/'), "/", lastPart.SubstringBefore('.'));
            if (path[^1] != '/' && !lastPart.Contains('.'))
                path += "." + GameFile.Ue4PackageExtensions[0];

            var root = path.SubstringBefore('/');
            if (root.Equals(GameName, StringComparison.OrdinalIgnoreCase))
            {
                return comparisonType == StringComparison.OrdinalIgnoreCase ? path.ToLowerInvariant() : path;
            }

            if (root.Equals("Game", comparisonType) || root.Equals("Engine", comparisonType))
            {
                var gameName = root == "Engine" ? "Engine" : GameName;
                var p = path.SubstringAfter('/').SubstringBefore('/');
                if (p.Contains('.'))
                {
                    var ret = string.Concat(gameName, "/Content/", path.SubstringAfter('/'));
                    return comparisonType == StringComparison.OrdinalIgnoreCase ? ret.ToLowerInvariant() : ret;
                }

                if (p.Equals("Config", comparisonType) ||
                    p.Equals("Content", comparisonType) ||
                    p.Equals("Plugins", comparisonType))
                {
                    var ret = string.Concat(gameName, '/', path.SubstringAfter('/'));
                    return comparisonType == StringComparison.OrdinalIgnoreCase ? ret.ToLowerInvariant() : ret;
                }
                else
                {
                    var ret = string.Concat(gameName, "/Content/", path.SubstringAfter('/'));
                    return comparisonType == StringComparison.OrdinalIgnoreCase ? ret.ToLowerInvariant() : ret;
                }
            }

            if (VirtualPaths.TryGetValue(comparisonType == StringComparison.OrdinalIgnoreCase ? root.ToLowerInvariant() : root, out var use))
            {
                var ret = string.Concat(use, "/Content/", path.SubstringAfter('/'));
                return comparisonType == StringComparison.OrdinalIgnoreCase ? ret.ToLowerInvariant() : ret;
            }
            else
            {
                var ret = string.Concat(GameName, $"/Plugins/{(GameName.ToLowerInvariant().Equals("fortnitegame") ? "GameFeatures/" : "")}{root}/Content/", path.SubstringAfter('/'));
                return comparisonType == StringComparison.OrdinalIgnoreCase ? ret.ToLowerInvariant() : ret;
            }
        }

        #region SaveAsset Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] SaveAsset(string path) => this[path].Read();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TrySaveAsset(string path, out byte[] data)
        {
            if (!TryFindGameFile(path, out var file))
            {
                data = default;
                return false;
            }

            return file.TryRead(out data);
        }

        public virtual async Task<byte[]> SaveAssetAsync(string path) => await Task.Run(() => SaveAsset(path));
        public virtual async Task<byte[]?> TrySaveAssetAsync(string path) => await Task.Run(() =>
        {
            TrySaveAsset(path, out var data);
            return data;
        });

        #endregion

        #region CreateReader Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual FArchive CreateReader(string path) => this[path].CreateReader();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryCreateReader(string path, out FArchive reader)
        {
            if (!TryFindGameFile(path, out var file))
            {
                reader = default;
                return false;
            }

            return file.TryCreateReader(out reader);
        }

        public virtual async Task<FArchive> CreateReaderAsync(string path) => await Task.Run(() => CreateReader(path));
        public virtual async Task<FArchive?> TryCreateReaderAsync(string path) => await Task.Run(() =>
        {
            TryCreateReader(path, out var reader);
            return reader;
        });

        #endregion

        #region LoadPackage Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IPackage LoadPackage(string path) => LoadPackage(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IPackage LoadPackage(GameFile file) => LoadPackageAsync(file).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IoPackage LoadPackage(FPackageId id) => (IoPackage) LoadPackage(FilesById[id]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadPackage(string path, out IPackage package)
        {
            if (!TryFindGameFile(path, out var file))
            {
                package = default;
                return false;
            }

            return TryLoadPackage(file, out package);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadPackage(GameFile file, out IPackage package)
        {
            package = TryLoadPackageAsync(file).Result;
            return package != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadPackage(FPackageId id, out IoPackage package)
        {
            if (FilesById.TryGetValue(id, out var file) && TryLoadPackage(file, out IPackage loaded))
            {
                package = (IoPackage) loaded;
                return true;
            }

            package = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<IPackage> LoadPackageAsync(string path) => await LoadPackageAsync(this[path]);
        public virtual async Task<IPackage> LoadPackageAsync(GameFile file)
        {
            if (!file.IsUE4Package) throw new ArgumentException("File must be a package to be a loaded as one", nameof(file));
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.CreateReaderAsync();
            var uexpTask = uexpFile?.CreateReaderAsync();
            var ubulkTask = ubulkFile?.CreateReaderAsync();
            var uptnlTask = uptnlFile?.CreateReaderAsync();
            var uasset = await uassetTask;
            var uexp = uexpTask != null ? await uexpTask : null;
            var ubulk = ubulkTask != null ? await ubulkTask : null;
            var uptnl = uptnlTask != null ? await uptnlTask : null;

            if (file is FPakEntry)
            {
                return new Package(uasset, uexp, ubulk, uptnl, this, MappingsForThisGame, UseLazySerialization);
            }

            if (this is not IVfsFileProvider vfsFileProvider || vfsFileProvider.GlobalData == null)
            {
                throw new ParserException("Found IoStore Package but global data is missing, can't serialize");
            }

            var ioEntry = (FIoStoreEntry) file;
            var containerHeader = ((IoStoreReader) ioEntry.Vfs).ContainerHeader;
            var storeEntry = containerHeader?.StoreEntries[Array.IndexOf(containerHeader.PackageIds, new FPackageId(ioEntry.ChunkId.ChunkId))];
            return new IoPackage(uasset, vfsFileProvider.GlobalData, storeEntry, ubulk, uptnl, this, MappingsForThisGame);
        }

        public virtual async Task<IPackage?> TryLoadPackageAsync(string path)
        {
            if (!TryFindGameFile(path, out var file))
            {
                return null;
            }

            return await TryLoadPackageAsync(file).ConfigureAwait(false);
        }

        public virtual async Task<IPackage?> TryLoadPackageAsync(GameFile file)
        {
            if (!file.IsUE4Package)
                return null;
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.TryCreateReaderAsync().ConfigureAwait(false);
            var uexpTask = uexpFile?.TryCreateReaderAsync().ConfigureAwait(false);
            var lazyUbulk = ubulkFile != null
                ? new Lazy<FArchive?>(() => ubulkFile.TryCreateReader(out var reader) ? reader : null, LazyThreadSafetyMode.ExecutionAndPublication)
                : null;

            var lazyUptnl = uptnlFile != null
                ? new Lazy<FArchive?>(() => uptnlFile.TryCreateReader(out var reader) ? reader : null, LazyThreadSafetyMode.ExecutionAndPublication)
                : null;

            var uasset = await uassetTask;
            if (uasset == null)
                return null;
            var uexp = uexpTask != null ? await uexpTask.Value : null;

            try
            {
                if (file is FPakEntry)
                {
                    return new Package(uasset, uexp, lazyUbulk, lazyUptnl, this, MappingsForThisGame, UseLazySerialization);
                }

                if (this is not IVfsFileProvider vfsFileProvider || vfsFileProvider.GlobalData == null)
                {
                    return null;
                }

                var ioEntry = (FIoStoreEntry) file;
                var containerHeader = ((IoStoreReader) ioEntry.Vfs).ContainerHeader;
                var storeEntry = containerHeader?.StoreEntries[Array.IndexOf(containerHeader.PackageIds, new FPackageId(ioEntry.ChunkId.ChunkId))];
                return new IoPackage(uasset, vfsFileProvider.GlobalData, storeEntry, lazyUbulk, lazyUptnl, this, MappingsForThisGame);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region SavePackageMethods

        public virtual IReadOnlyDictionary<string, byte[]> SavePackage(string path) => SavePackage(this[path]);

        public virtual IReadOnlyDictionary<string, byte[]> SavePackage(GameFile file) => SavePackageAsync(file).Result;

        public virtual bool TrySavePackage(string path, out IReadOnlyDictionary<string, byte[]> package)
        {
            if (!TryFindGameFile(path, out var file))
            {
                package = default;
                return false;
            }

            return TrySavePackage(file, out package);
        }

        public virtual bool TrySavePackage(GameFile file, out IReadOnlyDictionary<string, byte[]> package)
        {
            package = TrySavePackageAsync(file).Result;
            return package != null;
        }

        public virtual async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(string path) =>
            await SavePackageAsync(this[path]);

        public virtual async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(GameFile file)
        {
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.ReadAsync();
            var uexpTask = uexpFile?.ReadAsync();
            var ubulkTask = ubulkFile?.ReadAsync();
            var uptnlTask = uptnlFile?.ReadAsync();
            var dict = new Dictionary<string, byte[]>
            {
                { file.Path, await uassetTask }
            };
            var uexp = uexpTask != null ? await uexpTask : null;
            var ubulk = ubulkTask != null ? await ubulkTask : null;
            var uptnl = uptnlTask != null ? await uptnlTask : null;
            if (uexpFile != null && uexp != null)
                dict[uexpFile.Path] = uexp;
            if (ubulkFile != null && ubulk != null)
                dict[ubulkFile.Path] = ubulk;
            if (uptnlFile != null && uptnl != null)
                dict[uptnlFile.Path] = uptnl;
            return dict;
        }

        public virtual async Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(string path)
        {
            if (!TryFindGameFile(path, out var file))
            {
                return null;
            }

            return await TrySavePackageAsync(file).ConfigureAwait(false);
        }

        public virtual async Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(GameFile file)
        {
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.TryReadAsync().ConfigureAwait(false);
            var uexpTask = uexpFile?.TryReadAsync().ConfigureAwait(false);
            var ubulkTask = ubulkFile?.TryReadAsync().ConfigureAwait(false);
            var uptnlTask = uptnlFile?.TryReadAsync().ConfigureAwait(false);

            var uasset = await uassetTask;
            if (uasset == null)
                return null;
            var uexp = uexpTask != null ? await uexpTask.Value : null;
            var ubulk = ubulkTask != null ? await ubulkTask.Value : null;
            var uptnl = uptnlTask != null ? await uptnlTask.Value : null;

            var dict = new Dictionary<string, byte[]>
            {
                { file.Path, uasset }
            };
            if (uexpFile != null && uexp != null)
                dict[uexpFile.Path] = uexp;
            if (ubulkFile != null && ubulk != null)
                dict[ubulkFile.Path] = ubulk;
            if (uptnlFile != null && uptnl != null)
                dict[uptnlFile.Path] = uptnl;
            return dict;
        }

        #endregion

        #region LoadObject Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual UObject LoadObject(string? objectPath) => LoadObjectAsync(objectPath).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadObject(string? objectPath, out UObject export)
        {
            export = TryLoadObjectAsync(objectPath).Result;
            return export != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual T LoadObject<T>(string? objectPath) where T : UObject => LoadObjectAsync<T>(objectPath).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryLoadObject<T>(string? objectPath, out T export) where T : UObject
        {
            export = TryLoadObjectAsync<T>(objectPath).Result;
            return export != null;
        }

        public virtual async Task<UObject> LoadObjectAsync(string? objectPath)
        {
            if (objectPath == null) throw new ArgumentException("ObjectPath can't be null", nameof(objectPath));
            var packagePath = objectPath;
            string objectName;
            var dotIndex = packagePath.IndexOf('.');
            if (dotIndex == -1) // use the package name as object name
            {
                objectName = packagePath.SubstringAfterLast('/');
            }
            else // packagePath.objectName
            {
                objectName = packagePath.Substring(dotIndex + 1);
                packagePath = packagePath.Substring(0, dotIndex);
            }

            var pkg = await LoadPackageAsync(packagePath);
            return pkg.GetExport(objectName, IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        public virtual async Task<UObject?> TryLoadObjectAsync(string? objectPath)
        {
            if (objectPath == null || objectPath.Equals("None", IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return null;
            var packagePath = objectPath;
            string objectName;
            var dotIndex = packagePath.IndexOf('.');
            if (dotIndex == -1) // use the package name as object name
            {
                objectName = packagePath.SubstringAfterLast('/');
            }
            else // packagePath.objectName
            {
                objectName = packagePath.Substring(dotIndex + 1);
                packagePath = packagePath.Substring(0, dotIndex);
            }

            var pkg = await TryLoadPackageAsync(packagePath);
            return pkg?.GetExportOrNull(objectName, IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<T> LoadObjectAsync<T>(string? objectPath) where T : UObject =>
            await LoadObjectAsync(objectPath) as T ??
            throw new ParserException("Loaded object but it was of wrong type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<T?> TryLoadObjectAsync<T>(string? objectPath) where T : UObject =>
            await TryLoadObjectAsync(objectPath) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IEnumerable<UObject> LoadObjectExports(string? objectPath)
        {
            if (objectPath == null) throw new ArgumentException("ObjectPath can't be null", nameof(objectPath));

            var pkg = LoadPackage(objectPath);
            return pkg.GetExports();
        }

        #endregion
    }
}