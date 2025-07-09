using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.ApexMobile.Encryption.Aes;
using CUE4Parse.GameTypes.DBD.Encryption.Aes;
using CUE4Parse.GameTypes.DeltaForce.Encryption.Aes;
using CUE4Parse.GameTypes.DreamStar.Encryption.Aes;
using CUE4Parse.GameTypes.FSR.Encryption.Aes;
using CUE4Parse.GameTypes.FunkoFusion.Encryption.Aes;
using CUE4Parse.GameTypes.INikki.Encryption.Aes;
using CUE4Parse.GameTypes.MindsEye.Encryption.Aes;
using CUE4Parse.GameTypes.MJS.Encryption.Aes;
using CUE4Parse.GameTypes.NetEase.MAR.Encryption.Aes;
using CUE4Parse.GameTypes.NFS.Mobile.Encryption.Aes;
using CUE4Parse.GameTypes.PAXDEI.Encryption.Aes;
using CUE4Parse.GameTypes.PMA.Encryption.Aes;
using CUE4Parse.GameTypes.Rennsport.Encryption.Aes;
using CUE4Parse.GameTypes.SD.Encryption.Aes;
using CUE4Parse.GameTypes.Snowbreak.Encryption.Aes;
using CUE4Parse.GameTypes.Splitgate2.Encryption.Aes;
using CUE4Parse.GameTypes.THPS.Encryption.Aes;
using CUE4Parse.GameTypes.UDWN.Encryption.Aes;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using OffiUtils;

namespace CUE4Parse.FileProvider.Vfs
{
    public abstract class AbstractVfsFileProvider : AbstractFileProvider, IVfsFileProvider
    {
        protected readonly ConcurrentDictionary<IAesVfsReader, object?> _unloadedVfs = new ();
        public IReadOnlyCollection<IAesVfsReader> UnloadedVfs => (IReadOnlyCollection<IAesVfsReader>) _unloadedVfs.Keys;

        private readonly ConcurrentDictionary<IAesVfsReader, object?> _mountedVfs = new ();
        public IReadOnlyCollection<IAesVfsReader> MountedVfs => (IReadOnlyCollection<IAesVfsReader>) _mountedVfs.Keys;

        private readonly ConcurrentDictionary<FGuid, FAesKey> _keys = new ();
        public IReadOnlyDictionary<FGuid, FAesKey> Keys => _keys;

        protected readonly ConcurrentDictionary<FGuid, object?> _requiredKeys = new ();
        public IReadOnlyCollection<FGuid> RequiredKeys => (IReadOnlyCollection<FGuid>) _requiredKeys.Keys;

        public IoGlobalData? GlobalData { get; private set; }

        public IReadOnlyDictionary<FPackageId, GameFile> FilesById => Files.ById;

        public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }
        public event EventHandler<int>? VfsRegistered;
        public event EventHandler<int>? VfsMounted;
        public event EventHandler<int>? VfsUnmounted;

        protected AbstractVfsFileProvider(VersionContainer? versions = null, StringComparer? pathComparer = null) : base(versions, pathComparer)
        {
            CustomEncryption = versions?.Game switch
            {
                EGame.GAME_ApexLegendsMobile => ApexLegendsMobileAes.DecryptApexMobile,
                EGame.GAME_Snowbreak => SnowbreakAes.SnowbreakDecrypt,
                EGame.GAME_MarvelRivals => MarvelAes.MarvelDecrypt,
                EGame.GAME_Undawn => ToaaAes.ToaaDecrypt,
                EGame.GAME_DeadByDaylight => DBDAes.DbDDecrypt,
                EGame.GAME_PaxDei => PaxDeiAes.PaxDeiDecrypt,
                EGame.GAME_3on3FreeStyleRebound => FreeStyleReboundAes.FSRDecrypt,
                EGame.GAME_DreamStar => DreamStarAes.DreamStarDecrypt,
                EGame.GAME_DeltaForceHawkOps => DeltaForceAes.DeltaForceDecrypt,
                EGame.GAME_PromiseMascotAgency => PMAAes.PMADecrypt,
                EGame.GAME_MonsterJamShowdown => MonsterJamShowdownAes.MonsterJamShowdownDecrypt,
                EGame.GAME_MotoGP25 => MotoGP25Aes.MotoGP25Decrypt,
                EGame.GAME_Rennsport => RennsportAes.RennsportDecrypt,
                EGame.GAME_FunkoFusion => FunkoFusionAes.FunkoFusionDecrypt,
                EGame.GAME_TonyHawkProSkater12 or EGame.GAME_TonyHawkProSkater34 => THPS12Aes.THPS12Decrypt,
                EGame.GAME_InfinityNikki => InfinityNikkiAes.InfinityNikkiDecrypt,
                EGame.GAME_Spectre => SpectreDivideAes.SpectreDecrypt,
                EGame.GAME_Splitgate2 => Splitgate2Aes.Splitgate2Decrypt,
                EGame.GAME_MindsEye => MindsEyeAes.MindsEyeDecrypt,
                EGame.GAME_NeedForSpeedMobile => NFSMobileAes.NFSMobileDecrypt,
                _ => null
            };
        }

        public abstract void Initialize();

        public void RegisterVfs(FileInfo file) => RegisterVfs(file.FullName);
        public void RegisterVfs(string file) => RegisterRandomAccessVfs(new FRandomAccessFileStreamArchive(file, Versions), null, openPath => new FRandomAccessFileStreamArchive(openPath, Versions));

        public void RegisterVfs(FRandomAccessFileStreamArchive[] stream, Func<string, FArchive>? openContainerStreamFunc = null)
            => RegisterRandomAccessVfs(stream[0], stream.Length > 1 ? stream[1] : null, openContainerStreamFunc);
        public void RegisterVfs(FRandomAccessStreamArchive[] stream, Func<string, FArchive>? openContainerStreamFunc = null)
            => RegisterRandomAccessVfs(stream[0], stream.Length > 1 ? stream[1] : null, openContainerStreamFunc);
        public void RegisterVfs(string file, RandomAccessStream[] stream, Func<string, FArchive>? openContainerStreamFunc = null)
            => RegisterRandomAccessVfs(new FRandomAccessStreamArchive(file, stream[0], Versions), stream.Length > 1 ? stream[1] : null, openContainerStreamFunc);

        public void RegisterVfs(string file, Stream[] stream, Func<string, FArchive>? openContainerStreamFunc = null)
            => RegisterVfs(new FStreamArchive(file, stream[0], Versions), stream.Length > 1 ? stream[1] : null, openContainerStreamFunc);

        public void RegisterVfs(string[] filePaths)
            => RegisterRandomAccessVfs(
                new FRandomAccessFileStreamArchive(filePaths[0], Versions),
                filePaths.Length > 1 ? new FRandomAccessFileStreamArchive(filePaths[1], Versions) : null,
                openPath => new FRandomAccessFileStreamArchive(openPath, Versions));

        public void RegisterVfs(FileInfo[] fileInfos)
            => RegisterRandomAccessVfs(
                new FRandomAccessFileStreamArchive(fileInfos[0], Versions),
                fileInfos.Length > 1 ? new FRandomAccessFileStreamArchive(fileInfos[1], Versions) : null,
                openPath => new FRandomAccessFileStreamArchive(openPath, Versions));

        public void RegisterVfs(FArchive archive, Stream? stream, Func<string, FArchive>? openContainerStreamFunc = null)
        {
            try
            {
                AbstractAesVfsReader reader;
                switch (archive.Name.SubstringAfterLast('.').ToUpper())
                {
                    case "PAK":
                        reader = new PakFileReader(archive);
                        break;
                    case "UTOC":
                        openContainerStreamFunc ??= it => new FStreamArchive(it, stream!, Versions);
                        reader = new IoStoreReader(archive, openContainerStreamFunc);
                        break;
                    default:
                        return;
                }
                PostLoadReader(reader);
            }
            catch (Exception e)
            {
                Log.Warning(e.ToString());
            }
        }
        public void RegisterRandomAccessVfs(FArchive pakOrUtocArchive, FArchive? utocArchive, Func<string, FArchive>? openContainerStreamFunc = null)
        {
            try
            {
                pakOrUtocArchive.Versions = Versions;
                if (utocArchive is not null)
                    utocArchive.Versions = Versions;

                AbstractAesVfsReader reader;
                switch (pakOrUtocArchive.Name.SubstringAfterLast('.').ToUpper())
                {
                    case "PAK":
                        reader = new PakFileReader(pakOrUtocArchive);
                        break;
                    case "UTOC":
                        openContainerStreamFunc ??= _ => utocArchive!;
                        reader = new IoStoreReader(pakOrUtocArchive, openContainerStreamFunc);
                        break;
                    default:
                        return;
                }
                PostLoadReader(reader, false);
            }
            catch (Exception e)
            {
                Log.Warning(e.ToString());
            }
        }
        public void RegisterRandomAccessVfs(FArchive pakOrUtocArchive, RandomAccessStream? utocStream, Func<string, FArchive>? openContainerStreamFunc = null)
        {
            try
            {
                pakOrUtocArchive.Versions = Versions;

                AbstractAesVfsReader reader;
                switch (pakOrUtocArchive.Name.SubstringAfterLast('.').ToUpper())
                {
                    case "PAK":
                        reader = new PakFileReader(pakOrUtocArchive);
                        break;
                    case "UTOC":
                        openContainerStreamFunc ??= it => new FRandomAccessStreamArchive(it, utocStream!, Versions);
                        reader = new IoStoreReader(pakOrUtocArchive, openContainerStreamFunc);
                        break;
                    default:
                        return;
                }
                PostLoadReader(reader, false);
            }
            catch (Exception e)
            {
                Log.Warning(e.ToString());
            }
        }
        public async Task RegisterVfs(IoChunkToc chunkToc, IoStoreOnDemandOptions options)
        {
            var downloader = new IoStoreOnDemandDownloader(options);
            foreach (var container in chunkToc.Containers)
            {
                PostLoadReader(new IoStoreOnDemandReader(
                    new FStreamArchive($"{container.ContainerName}.utoc", await downloader.Download($"{container.UTocHash.ToString().ToLower()}.utoc"), Versions),
                    container.Entries, downloader));
            }
        }

        private void PostLoadReader(AbstractAesVfsReader reader, bool isConcurrent = true)
        {
            if (reader.IsEncrypted)
                _requiredKeys.TryAdd(reader.EncryptionKeyGuid, null);

            _unloadedVfs[reader] = null;
            reader.IsConcurrent = isConcurrent;
            if (!(reader.Game == EGame.GAME_MarvelRivals && reader is IoStoreReader)) // no custom encryption for MR IoStore
            {
                reader.CustomEncryption = CustomEncryption;
            }

            VfsRegistered?.Invoke(reader, _unloadedVfs.Count);
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
                        reader.MountTo(Files, PathComparer, VfsMounted);
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
                    if (reader.Game == EGame.GAME_FragPunk && reader.Name.Contains("global")) reader.AesKey = key;
                    VerifyGlobalData(reader);

                    if (!reader.HasDirectoryIndex)
                        continue;

                    tasks.AddLast(Task.Run(() =>
                    {
                        try
                        {
                            reader.MountTo(Files, PathComparer, key, VfsMounted);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAesVfsReader GetArchive(string archiveName, StringComparison comparison = StringComparison.Ordinal)
        {
            var predicate = (IAesVfsReader x) => x.Name.Equals(archiveName, comparison);
            return MountedVfs.FirstOrDefault(predicate) ??
                   UnloadedVfs.FirstOrDefault(predicate) ??
                   throw new KeyNotFoundException($"There is no archive file with the name \"{archiveName}\"");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetArchive(string archiveName, [MaybeNullWhen(false)] out IAesVfsReader archive, StringComparison comparison = StringComparison.Ordinal)
        {
            try
            {
                archive = GetArchive(archiveName, comparison);
            }
            catch
            {
                archive = null;
            }
            return archive != null;
        }

        public GameFile this[string path, string archiveName, StringComparison comparison = StringComparison.Ordinal] => this[path, GetArchive(archiveName, comparison)];
        public GameFile this[string path, IAesVfsReader archive]
            => TryGetGameFile(path, archive.Files, out var file)
                ? file
                : throw new KeyNotFoundException($"There is no game file with the path \"{path}\" in \"{archive.Name}\"");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGameFile(string path, string archiveName, [MaybeNullWhen(false)] out GameFile file, StringComparison comparison = StringComparison.Ordinal)
        {
            try
            {
                file = this[path, archiveName, comparison];
            }
            catch
            {
                file = null;
            }
            return file != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] SaveAsset(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal)
            => SaveAsset(path, GetArchive(archiveName, comparison));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] SaveAsset(string path, IAesVfsReader archive) => SaveAsset(this[path, archive]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FArchive CreateReader(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal)
            => CreateReader(path, GetArchive(archiveName, comparison));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FArchive CreateReader(string path, IAesVfsReader archive) => this[path, archive].CreateReader();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IoPackage LoadPackage(FPackageId id) => (IoPackage) LoadPackage(FilesById[id]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadPackage(FPackageId id, [MaybeNullWhen(false)] out IoPackage ioPackage)
        {
            if (FilesById.TryGetValue(id, out var file) && TryLoadPackage(file, out var package))
            {
                ioPackage = (IoPackage) package;
                return true;
            }

            ioPackage = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPackage LoadPackage(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal)
            => LoadPackage(path, GetArchive(archiveName, comparison));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPackage LoadPackage(string path, IAesVfsReader archive) => LoadPackage(this[path, archive]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyDictionary<string, byte[]> SavePackage(string path, string archiveName, StringComparison comparison = StringComparison.Ordinal)
            => SavePackage(path, GetArchive(archiveName, comparison));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyDictionary<string, byte[]> SavePackage(string path, IAesVfsReader archive)
            => SavePackage(this[path, archive]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySavePackage(string path, string archiveName, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data, StringComparison comparison = StringComparison.Ordinal)
        {
            if (TryGetGameFile(path, archiveName, out var file, comparison))
            {
                return TrySavePackage(file, out data);
            }

            data = null;
            return false;
        }

        /// <summary>
        /// load .ini files and verify the validity of the main encryption key against them
        /// in cases where archives are not encrypted, but their packages are, that is one way to tell if the key is correct
        /// if the key is not correct, archives will be removed from the pool of mounted archives no matter how many encrypted packages they have
        /// </summary>
        public void PostMount()
        {
            var workingAes = LoadIniConfigs();
            if (workingAes || DefaultGame.EncryptionKeyGuid is null) return;

            var vfsToVerify = _mountedVfs.Keys
                .Where(it => it is {IsEncrypted: false, EncryptedFileCount: > 0})
                .GroupBy(it => it.EncryptionKeyGuid);

            foreach (var group in vfsToVerify)
            {
                if (group.Key != DefaultGame.EncryptionKeyGuid) continue;
                foreach (var reader in group)
                {
                    _mountedVfs.TryRemove(reader, out _);
                    _unloadedVfs[reader] = null;
                    VfsUnmounted?.Invoke(reader, _unloadedVfs.Count);
                }
                _keys.TryRemove(group.Key, out _);
                _requiredKeys[group.Key] = null;
            }
        }

        private void VerifyGlobalData(IAesVfsReader reader)
        {
            if (GlobalData != null || reader is not IoStoreReader ioStoreReader) return;

            if (ioStoreReader.Name.Equals("global.utoc", StringComparison.OrdinalIgnoreCase) ||
                ioStoreReader.Name.Equals("global_console_win.utoc", StringComparison.OrdinalIgnoreCase))
            {
                GlobalData = new IoGlobalData(ioStoreReader);
            }
        }

        public void UnloadAllVfs()
        {
            Files.Clear();
            foreach (var reader in _mountedVfs.Keys)
            {
                _keys.TryRemove(reader.EncryptionKeyGuid, out _);
                _requiredKeys[reader.EncryptionKeyGuid] = null;
                _mountedVfs.TryRemove(reader, out _);
                _unloadedVfs[reader] = null;
                VfsUnmounted?.Invoke(reader, _unloadedVfs.Count);
            }
        }
        public void UnloadNonStreamedVfs()
        {
            var onDemandFiles = new Dictionary<string, GameFile>(PathComparer);
            foreach (var (path, vfs) in Files)
                if (vfs is StreamedGameFile or OsGameFile)
                    onDemandFiles[path] = vfs;

            UnloadAllVfs();
            Files.AddFiles(onDemandFiles);
        }

        public override void Dispose()
        {
            base.Dispose();

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
