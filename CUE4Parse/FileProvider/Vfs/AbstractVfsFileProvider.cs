using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Vfs;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider.Vfs
{
    public abstract class AbstractVfsFileProvider : AbstractFileProvider, IVfsFileProvider
    {
        protected FileProviderDictionary _files;
        public override IReadOnlyDictionary<string, GameFile> Files => _files;

        protected ConcurrentDictionary<IAesVfsReader, object?> _unloadedVfs =
            new ConcurrentDictionary<IAesVfsReader, object?>();

        public IReadOnlyCollection<IAesVfsReader> UnloadedVfs =>
            (IReadOnlyCollection<IAesVfsReader>) _unloadedVfs.Keys;

        protected ConcurrentDictionary<IAesVfsReader, object?> _mountedVfs =
            new ConcurrentDictionary<IAesVfsReader, object?>();

        public IReadOnlyCollection<IAesVfsReader> MountedVfs => (IReadOnlyCollection<IAesVfsReader>) _mountedVfs.Keys;

        protected ConcurrentDictionary<FGuid, FAesKey> _keys = new ConcurrentDictionary<FGuid, FAesKey>();
        public IReadOnlyDictionary<FGuid, FAesKey> Keys => _keys;
        protected ConcurrentDictionary<FGuid, object?> _requiredKeys = new ConcurrentDictionary<FGuid, object?>();
        public IReadOnlyCollection<FGuid> RequiredKeys => (IReadOnlyCollection<FGuid>) _requiredKeys.Keys;

        protected AbstractVfsFileProvider(bool isCaseInsensitive = false,
            UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST) : base(isCaseInsensitive,
            ver, game)
        {
            _files = new FileProviderDictionary(IsCaseInsensitive);
        }

        public IEnumerable<IAesVfsReader> UnloadedVfsByGuid(FGuid guid) =>
            _unloadedVfs.Keys.Where(it => it.EncryptionKeyGuid == guid);

        public int Mount() => MountAsync().Result;

        public async Task<int> MountAsync()
        {
            var countNewMounts = 0;
            var tasks = new LinkedList<Task>();
            foreach (var it in _unloadedVfs)
            {
                var reader = it.Key;
                if (reader.IsEncrypted)
                    continue;
                tasks.AddLast(Task.Run(() =>
                {
                    try
                    {
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
                        Log.Warning(e,
                            $"Uncaught exception while loading pak file {reader.Name.SubstringAfterLast('/')}");
                    }

                    return null;
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return countNewMounts;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SubmitKey(FGuid guid, FAesKey key) => SubmitKeys(new Dictionary<FGuid, FAesKey> {[guid] = key});

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<int> SubmitKeyAsync(FGuid guid, FAesKey key) =>
            await SubmitKeysAsync(new Dictionary<FGuid, FAesKey> {[guid] = key}).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SubmitKeys(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys) => SubmitKeysAsync(keys).Result;

        public async Task<int> SubmitKeysAsync(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys)
        {
            var countNewMounts = 0;
            var tasks = new LinkedList<Task<IAesVfsReader?>>();
            foreach (var it in keys)
            {
                var guid = it.Key;
                if (!_requiredKeys.ContainsKey(guid)) continue;
                var key = it.Value;
                foreach (var reader in UnloadedVfsByGuid(guid))
                {
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
                            Log.Warning(e,
                                $"Uncaught exception while loading pak file {reader.Name.SubstringAfterLast('/')}");
                        }

                        return null;
                    }));
                }
            }

            var completed = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var it in completed)
            {
                var key = it?.AesKey;
                if (it != null && key != null)
                {
                    _requiredKeys.TryRemove(it.EncryptionKeyGuid, out _);
                    _keys.TryAdd(it.EncryptionKeyGuid, key);
                }
            }

            return countNewMounts;
        }

        public void Dispose()
        {
            foreach (var pak in _mountedVfs)
            {
                pak.Key.Dispose();
            }
        }
    }
}