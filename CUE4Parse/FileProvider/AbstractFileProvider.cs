using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.FileProvider
{
    public abstract class AbstractFileProvider : IFileProvider
    {
        protected static readonly ILogger Log = Serilog.Log.ForContext<IFileProvider>();

        public UE4Version Ver { get; set; }
        public EGame Game { get; set; }
        public abstract IReadOnlyDictionary<string, GameFile> Files { get; }
        public bool IsCaseInsensitive { get; }

        protected AbstractFileProvider(bool isCaseInsensitive = false, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
        {
            IsCaseInsensitive = isCaseInsensitive;
            Ver = ver;
            Game = game;
        }

        public string GameName =>
            Files.Keys
                .FirstOrDefault(it => it.SubstringBefore('/').EndsWith("game", StringComparison.OrdinalIgnoreCase))
                ?.SubstringBefore("game", StringComparison.OrdinalIgnoreCase) ?? string.Empty;

        public GameFile this[string path] => Files[FixPath(path)];

        public bool TryFindGameFile(string path, out GameFile file) => Files.TryGetValue(FixPath(path), out file);

        public string FixPath(string path)
        {
            var comparisonType = IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            path = path.Replace('\\', '/');
            if (path[0] == '/')
                path = path.Substring(1);
            var lastPart = path.SubstringAfterLast('/');
            // This part is only for FSoftObjectPaths and not really needed anymore internally, but it's still in here for user input
            if (lastPart.Contains('.') && lastPart.SubstringBefore('.') == lastPart.SubstringAfter('.'))
                path = string.Concat(path.SubstringBeforeLast('/'), "/", lastPart.SubstringBefore('.'));
            if (path[path.Length - 1] != '/' && !lastPart.Contains('.'))
                path += ".uasset";
            if (path.StartsWith("Game/", comparisonType))
            {
                var gameName = GameName;
                path = path switch
                {
                    var s when s.StartsWith("Game/Content", comparisonType) ||
                               s.StartsWith("Game/Config", comparisonType) ||
                               s.StartsWith("Game/Plugins", comparisonType) => string.Concat(gameName, path),
                    // For files at root level like Game/AssetRegistry.bin
                    var s when s.SubstringAfter('/').SubstringBefore('/').Contains('.') =>
                        string.Concat(gameName, path),
                    _ => string.Concat(gameName, "Game/Content/", path.Substring(5))
                };
            } else if (path.StartsWith("Engine/"))
            {
                path = path switch
                {
                    var s when s.StartsWith("Engine/Content", comparisonType) ||
                               s.StartsWith("Engine/Config", comparisonType) ||
                               s.StartsWith("Engine/Plugins", comparisonType) => path,
                    // For files at root level
                    var s when s.SubstringAfter('/').SubstringBefore('/').Contains('.') => path,
                    _ => string.Concat("Engine/Content/", path.Substring(7))
                };
            }

            return IsCaseInsensitive ? path.ToLowerInvariant() : path;
        }

        #region SaveAsset Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] SaveAsset(string path) => this[path].Read();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySaveAsset(string path, out byte[] data)
        {
            if (!TryFindGameFile(path, out var file))
            {
                data = default;
                return false;
            }

            return file.TryRead(out data);
        }

        public async Task<byte[]> SaveAssetAsync(string path) => await Task.Run(() => SaveAsset(path));
        public async Task<byte[]?> TrySaveAssetAsync(string path) => await Task.Run(() =>
        {
            TrySaveAsset(path, out var data);
            return data;
        });
        
        #endregion

        #region CreateReader Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FArchive CreateReader(string path) => this[path].CreateReader();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateReader(string path, out FArchive reader)
        {
            if (!TryFindGameFile(path, out var file))
            {
                reader = default;
                return false;
            }

            return file.TryCreateReader(out reader);
        }
        
        public async Task<FArchive> CreateReaderAsync(string path) => await Task.Run(() => CreateReader(path));
        public async Task<FArchive?> TryCreateReaderAsync(string path) => await Task.Run(() =>
        {
            TryCreateReader(path, out var reader);
            return reader;
        });
        
        #endregion

        #region LoadPackage Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPackage LoadPackage(string path) => LoadPackage(this[path]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPackage LoadPackage(GameFile file) => LoadPackageAsync(file).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackage(string path, out IPackage package)
        {
            if (!TryFindGameFile(path, out var file))
            {
                package = default;
                return false;
            }

            return TryLoadPackage(file, out package);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackage(GameFile file, out IPackage package)
        {
            package = TryLoadPackageAsync(file).Result;
            return package != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<IPackage> LoadPackageAsync(string path) => await LoadPackageAsync(this[path]);
        public async Task<IPackage> LoadPackageAsync(GameFile file)
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
            // TODO Decide which package parser to use
            if (uexp != null)
            {
                return new Package(uasset, uexp, 
                    ubulk, uptnl, this);
            }
            else
            {
                if (!(this is IVfsFileProvider vfsFileProvider) || vfsFileProvider.GlobalData == null)
                {
                    throw new ParserException("Found IoStore Package but global data is missing, can't serialize");
                }
                return new IoPackage(uasset, vfsFileProvider.GlobalData,
                    ubulk, uptnl, this);
            }
        }

        public async Task<IPackage?> TryLoadPackageAsync(string path)
        {
            if (!TryFindGameFile(path, out var file))
            {
                return null;
            }

            return await TryLoadPackageAsync(file).ConfigureAwait(false);
        }

        public async Task<IPackage?> TryLoadPackageAsync(GameFile file)
        {
            if (!file.IsUE4Package)
                return null;
            Files.TryGetValue(file.PathWithoutExtension + ".uexp", out var uexpFile);
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.TryCreateReaderAsync().ConfigureAwait(false);
            var uexpTask = uexpFile?.TryCreateReaderAsync().ConfigureAwait(false);
            var ubulkTask = ubulkFile?.TryCreateReaderAsync().ConfigureAwait(false);
            var uptnlTask = uptnlFile?.TryCreateReaderAsync().ConfigureAwait(false);

            var uasset = await uassetTask;
            if (uasset == null)
                return null;
            var uexp = uexpTask != null ? await uexpTask.Value : null;
            var ubulk = ubulkTask != null ? await ubulkTask.Value : null;
            var uptnl = uptnlTask != null ? await uptnlTask.Value : null;

            // TODO Decide which package parser to use
            try
            {
                if (uexp != null)
                {
                    return new Package(uasset, uexp, ubulk, uptnl, this);
                }
                else
                {
                    if (!(this is IVfsFileProvider vfsFileProvider) || vfsFileProvider.GlobalData == null)
                    {
                        return null;
                    }
                    return new IoPackage(uasset, vfsFileProvider.GlobalData, ubulk, uptnl, this);
                }
            }
            catch
            {
                return null;
            }
        }
        
        #endregion

        #region SavePackageMethods

        public IReadOnlyDictionary<string, byte[]> SavePackage(string path) => SavePackage(this[path]);

        public IReadOnlyDictionary<string, byte[]> SavePackage(GameFile file) => SavePackageAsync(file).Result;

        public bool TrySavePackage(string path, out IReadOnlyDictionary<string, byte[]> package)
        {
            if (!TryFindGameFile(path, out var file))
            {
                package = default;
                return false;
            }

            return TrySavePackage(file, out package);
        }

        public bool TrySavePackage(GameFile file, out IReadOnlyDictionary<string, byte[]> package)
        {
            package = TrySavePackageAsync(file).Result;
            return package != null;
        }

        public async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(string path) =>
            await SavePackageAsync(this[path]);

        public async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(GameFile file)
        {
            if (!file.IsUE4Package) throw new ArgumentException("File must be a package to be saved as one", nameof(file));
            var uexpFile = Files[file.PathWithoutExtension + ".uexp"];
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.ReadAsync();
            var uexpTask = uexpFile.ReadAsync();
            var ubulkTask = ubulkFile?.ReadAsync();
            var uptnlTask = uptnlFile?.ReadAsync();
            var dict = new Dictionary<string, byte[]>()
            {
                {file.Path, await uassetTask},
                {uexpFile.Path, await uexpTask}
            };
            var ubulk = ubulkTask != null ? await ubulkTask : null;
            var uptnl = uptnlTask != null ? await uptnlTask : null;
            if (ubulkFile != null && ubulk != null)
                dict[ubulkFile.Path] = ubulk;
            if (uptnlFile != null && uptnl != null)
                dict[uptnlFile.Path] = uptnl;
            return dict;
        }

        public async Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(string path)
        {
            if (!TryFindGameFile(path, out var file))
            {
                return null;
            }

            return await TrySavePackageAsync(file).ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<string, byte[]>?> TrySavePackageAsync(GameFile file)
        {
            if (!file.IsUE4Package || !TryFindGameFile(file.PathWithoutExtension + ".uexp", out var uexpFile))
                return null;
            Files.TryGetValue(file.PathWithoutExtension + ".ubulk", out var ubulkFile);
            Files.TryGetValue(file.PathWithoutExtension + ".uptnl", out var uptnlFile);
            var uassetTask = file.TryReadAsync().ConfigureAwait(false);
            var uexpTask = uexpFile.TryReadAsync().ConfigureAwait(false);
            var ubulkTask = ubulkFile?.TryReadAsync().ConfigureAwait(false);
            var uptnlTask = uptnlFile?.TryReadAsync().ConfigureAwait(false);

            var uasset = await uassetTask;
            var uexp = await uexpTask;
            if (uasset == null || uexp == null)
                return null;
            var ubulk = ubulkTask != null ? await ubulkTask.Value : null;
            var uptnl = uptnlTask != null ? await uptnlTask.Value : null;
            
            var dict = new Dictionary<string, byte[]>()
            {
                {file.Path, uasset},
                {uexpFile.Path, uexp}
            };
            if (ubulkFile != null && ubulk != null)
                dict[ubulkFile.Path] = ubulk;
            if (uptnlFile != null && uptnl != null)
                dict[uptnlFile.Path] = uptnl;
            return dict;
        }

        #endregion

        #region LoadObject Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport LoadObject(string? objectPath) => LoadObjectAsync(objectPath).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadObject(string? objectPath, out UExport export)
        {
            export = TryLoadObjectAsync(objectPath).Result;
            return export != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LoadObject<T>(string? objectPath) where T : UExport => LoadObjectAsync<T>(objectPath).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadObject<T>(string? objectPath, out T export) where T : UExport
        {
            export = TryLoadObjectAsync<T>(objectPath).Result;
            return export != null;
        }

        public async Task<UExport> LoadObjectAsync(string? objectPath)
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

        public async Task<UExport?> TryLoadObjectAsync(string? objectPath)
        {
            if (objectPath == null) return null;
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
        public async Task<T> LoadObjectAsync<T>(string? objectPath) where T : UExport =>
            await LoadObjectAsync(objectPath) as T ??
            throw new ParserException("Loaded object but it was of wrong type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T?> TryLoadObjectAsync<T>(string? objectPath) where T : UExport =>
            await TryLoadObjectAsync(objectPath) as T;

        #endregion
    }
}