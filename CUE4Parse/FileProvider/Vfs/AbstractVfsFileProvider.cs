using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileCache;
using CUE4Parse.UE4.VirtualFileCache.Manifest;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider.Vfs
{
    public abstract class AbstractVfsFileProvider : AbstractFileProvider, IVfsFileProvider
    {
        protected FileProviderDictionary _files;
        public override IReadOnlyDictionary<string, GameFile> Files => _files;
        public override IReadOnlyDictionary<FPackageId, GameFile> FilesById => _files.byId;

        protected readonly ConcurrentDictionary<IAesVfsReader, object?> _unloadedVfs = new ();
        public IReadOnlyCollection<IAesVfsReader> UnloadedVfs => (IReadOnlyCollection<IAesVfsReader>) _unloadedVfs.Keys;

        private readonly ConcurrentDictionary<IAesVfsReader, object?> _mountedVfs = new ();
        public IReadOnlyCollection<IAesVfsReader> MountedVfs => (IReadOnlyCollection<IAesVfsReader>) _mountedVfs.Keys;

        private readonly ConcurrentDictionary<FGuid, FAesKey> _keys = new ();
        public IReadOnlyDictionary<FGuid, FAesKey> Keys => _keys;

        protected readonly ConcurrentDictionary<FGuid, object?> _requiredKeys = new ();
        public IReadOnlyCollection<FGuid> RequiredKeys => (IReadOnlyCollection<FGuid>) _requiredKeys.Keys;

        public IoGlobalData? GlobalData { get; private set; }

        public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }

        protected AbstractVfsFileProvider(bool isCaseInsensitive = false, VersionContainer? versions = null) : base(isCaseInsensitive, versions)
        {
            _files = new FileProviderDictionary(isCaseInsensitive);
        }

        public abstract void Initialize();

        public void RegisterVfs(string file) => RegisterVfs(new FileInfo(file));
        public void RegisterVfs(FileInfo file) => RegisterVfs(file.FullName, new Stream[] { file.OpenRead() });
        public void RegisterVfs(string file, Stream[] stream, Func<string, FArchive>? openContainerStreamFunc = null)
        {
            try
            {
                AbstractAesVfsReader reader;
                switch (file.SubstringAfterLast('.').ToUpper())
                {
                    case "PAK":
                        reader = new PakFileReader(file, stream[0], Versions);
                        break;
                    case "UTOC":
                        openContainerStreamFunc ??= it => new FStreamArchive(it, stream[1], Versions);
                        reader = new IoStoreReader(file, stream[0], openContainerStreamFunc, EIoStoreTocReadOptions.ReadDirectoryIndex, Versions);
                        break;
                    default:
                        return;
                }

                if (reader.IsEncrypted && !_requiredKeys.ContainsKey(reader.EncryptionKeyGuid))
                    _requiredKeys[reader.EncryptionKeyGuid] = null;

                _unloadedVfs[reader] = null;
                reader.IsConcurrent = true;
                reader.CustomEncryption = CustomEncryption;
            }
            catch (Exception e)
            {
                Log.Warning(e.ToString());
            }
        }

        public int Mount() => MountAsync().Result;
        public async Task<int> MountAsync()
        {
            var countNewMounts = 0;
            var tasks = new LinkedList<Task>();
            foreach (var reader in _unloadedVfs.Keys)
            {
                VerifyGlobalData(reader);

                if (reader.IsEncrypted && CustomEncryption == null || !reader.HasDirectoryIndex)
                    continue;

                tasks.AddLast(Task.Run(() =>
                {
                    try
                    {
                        // Ensure that the custom encryption delegate specified for the provider is also used for the reader
                        reader.CustomEncryption = CustomEncryption;
                        reader.MountTo(_files, IsCaseInsensitive);
                        _unloadedVfs.TryRemove(reader, out _);
                        _mountedVfs[reader] = null;
                        Interlocked.Increment(ref countNewMounts);
                        return reader;
                    }
                    catch (InvalidAesKeyException)
                    {
                        // Ignore this
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, $"Uncaught exception while loading file {reader.Path.SubstringAfterLast('/')}");
                    }
                    return null;
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return countNewMounts;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SubmitKey(FGuid guid, FAesKey key) => SubmitKeys(new Dictionary<FGuid, FAesKey> {{ guid, key }});
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SubmitKeys(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys) => SubmitKeysAsync(keys).Result;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<int> SubmitKeyAsync(FGuid guid, FAesKey key)
            => await SubmitKeysAsync(new Dictionary<FGuid, FAesKey> {{ guid, key }}).ConfigureAwait(false);
        public async Task<int> SubmitKeysAsync(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys)
        {
            var countNewMounts = 0;
            var tasks = new LinkedList<Task<IAesVfsReader?>>();
            foreach (var (guid, key) in keys)
            {
                foreach (var reader in _unloadedVfs.Keys.Where(it => it.EncryptionKeyGuid == guid))
                {
                    VerifyGlobalData(reader);

                    if (!reader.HasDirectoryIndex)
                        continue;

                    tasks.AddLast(Task.Run(() =>
                    {
                        try
                        {
                            reader.MountTo(_files, IsCaseInsensitive, key);
                            _unloadedVfs.TryRemove(reader, out _);
                            _mountedVfs[reader] = null;
                            Interlocked.Increment(ref countNewMounts);
                            return reader;
                        }
                        catch (InvalidAesKeyException)
                        {
                            // Ignore this
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e, $"Uncaught exception while loading pak file {reader.Path.SubstringAfterLast('/')}");
                        }
                        return null;
                    }));
                }
            }

            var completed = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var it in completed)
            {
                var key = it?.AesKey;
                if (it == null || key == null) continue;
                _requiredKeys.TryRemove(it.EncryptionKeyGuid, out _);
                _keys.TryAdd(it.EncryptionKeyGuid, key);
            }

            return countNewMounts;
        }

        private void VerifyGlobalData(IAesVfsReader reader)
        {
            if (GlobalData != null || reader is not IoStoreReader ioStoreReader) return;
            if (ioStoreReader.Name.Equals("global.utoc", StringComparison.OrdinalIgnoreCase) || ioStoreReader.Name.Equals("global_console_win.utoc", StringComparison.OrdinalIgnoreCase))
            {
                GlobalData = new IoGlobalData(ioStoreReader);
            }
        }

        public int LoadVirtualCache()
        {
            var persistentDownloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), InternalGameName, "Saved/PersistentDownloadDir");
            if (!Directory.Exists(persistentDownloadDir)) return 0;

            var vfcMetadata = Path.Combine(persistentDownloadDir, "VFC", "vfc.meta");
            var manifestCacheFolder = new DirectoryInfo(Path.Combine(persistentDownloadDir, "ManifestCache"));
            if (!File.Exists(vfcMetadata) || !manifestCacheFolder.Exists)
                return 0;

            var cachedManifest = manifestCacheFolder.GetFiles("*.manifest");
            if (cachedManifest.Length <= 0)
                return 0;

            var vfc = new FFileTable(new FByteArchive("vfc.meta", File.ReadAllBytes(vfcMetadata)));
            var manifest = new OptimizedContentBuildManifest(
                File.ReadAllBytes(cachedManifest.OrderBy(f => f.LastWriteTime).Last().FullName));

            var onDemandFiles = new Dictionary<string, GameFile>();
            foreach ((var vfcHash, var dataReference) in vfc.FileMap)
            {
                if (!manifest.HashNameMap.TryGetValue(vfcHash.ToString(), out var filePath)) continue;

                var onDemandFile = new VfcGameFile(vfc.BlockFiles, dataReference, persistentDownloadDir, filePath, Versions);
                if (IsCaseInsensitive) onDemandFiles[onDemandFile.Path.ToLowerInvariant()] = onDemandFile;
                else onDemandFiles[onDemandFile.Path] = onDemandFile;
            }

            _files.AddFiles(onDemandFiles);
            return onDemandFiles.Count;
        }

        public void UnloadAllVfs()
        {
            _files.Clear();
            foreach (var reader in _mountedVfs.Keys)
            {
                _keys.TryRemove(reader.EncryptionKeyGuid, out _);
                _requiredKeys[reader.EncryptionKeyGuid] = null;
                _mountedVfs.TryRemove(reader, out _);
                _unloadedVfs[reader] = null;
            }
        }
        public void UnloadNonStreamedVfs()
        {
            var onDemandFiles = new Dictionary<string, GameFile>();
            foreach (var (path, vfs) in _files)
                if (vfs is StreamedGameFile)
                    onDemandFiles[path] = vfs;

            UnloadAllVfs();
            _files.AddFiles(onDemandFiles);
        }

        public void Dispose()
        {
            _files = new FileProviderDictionary(IsCaseInsensitive);
            foreach (var reader in UnloadedVfs) reader.Dispose();
            _unloadedVfs.Clear();
            foreach (var reader in MountedVfs) reader.Dispose();
            _mountedVfs.Clear();
            _keys.Clear();
            _requiredKeys.Clear();
            GlobalData = null;
        }
    }
}
