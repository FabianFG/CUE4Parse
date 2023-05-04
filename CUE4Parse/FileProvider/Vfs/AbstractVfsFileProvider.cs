using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileCache;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using EpicManifestParser.Objects;

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
            _files = new FileProviderDictionary(IsCaseInsensitive);
        }

        public IEnumerable<IAesVfsReader> UnloadedVfsByGuid(FGuid guid) => _unloadedVfs.Keys.Where(it => it.EncryptionKeyGuid == guid);
        public void UnloadAllVfs()
        {
            _files = new FileProviderDictionary(IsCaseInsensitive);
            foreach (var reader in _mountedVfs.Keys)
            {
                _keys.TryRemove(reader.EncryptionKeyGuid, out _);
                _requiredKeys[reader.EncryptionKeyGuid] = null;
                _mountedVfs.TryRemove(reader, out _);
                _unloadedVfs[reader] = null;
            }
        }

        public int Mount() => MountAsync().Result;
        public async Task<int> MountAsync()
        {
            var countNewMounts = 0;
            var tasks = new LinkedList<Task>();
            foreach (var reader in _unloadedVfs.Keys)
            {
                if (GlobalData == null && reader is IoStoreReader ioReader &&
                    (reader.Name.Equals("global.utoc", StringComparison.OrdinalIgnoreCase) || reader.Name.Equals("global_console_win.utoc", StringComparison.OrdinalIgnoreCase)))
                {
                    GlobalData = new IoGlobalData(ioReader);
                }

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
        public async Task<int> SubmitKeyAsync(FGuid guid, FAesKey key) =>
            await SubmitKeysAsync(new Dictionary<FGuid, FAesKey> {{ guid, key }}).ConfigureAwait(false);

        public async Task<int> SubmitKeysAsync(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys)
        {
            var countNewMounts = 0;
            var tasks = new LinkedList<Task<IAesVfsReader?>>();
            foreach (var it in keys)
            {
                var guid = it.Key;
                var key = it.Value;
                foreach (var reader in UnloadedVfsByGuid(guid))
                {
                    if (GlobalData == null && reader is IoStoreReader ioReader &&
                        (reader.Name.Equals("global.utoc", StringComparison.OrdinalIgnoreCase) || reader.Name.Equals("global_console_win.utoc", StringComparison.OrdinalIgnoreCase)))
                    {
                        GlobalData = new IoGlobalData(ioReader);
                    }

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

        public void LoadVirtualCache()
        {
            var persistentDownloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), GameName, "Saved/PersistentDownloadDir");
            if (!Directory.Exists(persistentDownloadDir)) return;

            var vfcMetadata = Path.Combine(persistentDownloadDir, "VFC", "vfc.meta");
            var cachedManifest = new DirectoryInfo(Path.Combine(persistentDownloadDir, "ManifestCache")).GetFiles("*.manifest");
            if (!File.Exists(vfcMetadata) || cachedManifest.Length <= 0)
                return;

            var vfc = new FFileTable(new FByteArchive("vfc.meta", File.ReadAllBytes(vfcMetadata)));
            var manifest = new Manifest(File.ReadAllBytes(cachedManifest[0].FullName));

            var onDemandFiles = new Dictionary<string, GameFile>();
            foreach (var fileManifest in manifest.FileManifests)
            {
                foreach ((var hash, var dataReference) in vfc.FileMap)
                {
                    if (fileManifest.Hash == hash.ToString())
                    {
                        foreach (var r in dataReference.Ranges)
                        {
                            var blockSize = vfc.BlockFiles.First(x => x.FileId == r.FileId).BlockSize;
                            using var fs = new FileStream(Path.Combine(persistentDownloadDir, "VFC", $"vfc_{r.FileId}.data"), FileMode.Open, FileAccess.Read, FileShare.Read);
                            fs.Seek(r.Range.StartIndex * blockSize, SeekOrigin.Begin);
                            var data = new byte[r.Range.NumBlocks * blockSize];
                            var read = fs.Read(data, 0, data.Length);
                            if (read == dataReference.TotalSize && !Files.ContainsKey(fileManifest.Name))
                            {
                                Directory.CreateDirectory(Path.Combine(persistentDownloadDir, "VFC", fileManifest.Name.SubstringBeforeLast('/')));
                                File.WriteAllBytes(Path.Combine(persistentDownloadDir, "VFC", fileManifest.Name), data);

                                var onDemandFile = new OsGameFile(Path.Combine(persistentDownloadDir, "VFC"), fileManifest.Name, dataReference.TotalSize, Versions);
                                if (IsCaseInsensitive) onDemandFiles[onDemandFile.Path.ToLowerInvariant()] = onDemandFile;
                                else onDemandFiles[onDemandFile.Path] = onDemandFile;
                            }
                        }
                    }
                }
            }

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
