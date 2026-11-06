using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.FileProvider.Vfs
{
    public class FileProviderDictionary : IReadOnlyDictionary<string, GameFile>
    {
        private readonly ConcurrentBag<KeyValuePair<long, IReadOnlyDictionary<string, GameFile>>> _indicesBag = [];

        private ConcurrentDictionary<FPackageId, GameFile>? _byId;
        private ConcurrentDictionary<FPackageId, GameFile>? _optionalById;
        private int _count;
        public IReadOnlyDictionary<FPackageId, GameFile> ById => GetPackageIndex();
        public IReadOnlyDictionary<FPackageId, GameFile> OptionalById => GetOptionalSegmentPackageIndex();

        private readonly KeyEnumerable _keys;
        public IEnumerable<string> Keys => _keys;

        private readonly ValueEnumerable _values;
        public IEnumerable<GameFile> Values => _values;

        public FileProviderDictionary()
        {
            _keys = new KeyEnumerable(this);
            _values = new ValueEnumerable(this);
        }

        internal void PreallocatePackageIndex(int capacity)
        {
            if (capacity <= 0 || Volatile.Read(ref _byId) is not null)
                return;

            var packageIndex = new ConcurrentDictionary<FPackageId, GameFile>(Environment.ProcessorCount, capacity);
            Interlocked.CompareExchange(ref _byId, packageIndex, null);
        }

        private ConcurrentDictionary<FPackageId, GameFile> GetPackageIndex()
        {
            var packageIndex = Volatile.Read(ref _byId);
            if (packageIndex is not null)
                return packageIndex;

            var newPackageIndex = new ConcurrentDictionary<FPackageId, GameFile>();
            return Interlocked.CompareExchange(ref _byId, newPackageIndex, null) ?? newPackageIndex;
        }

        private ConcurrentDictionary<FPackageId, GameFile> GetOptionalSegmentPackageIndex()
        {
            var packageIndex = Volatile.Read(ref _optionalById);
            if (packageIndex is not null)
                return packageIndex;

            var newPackageIndex = new ConcurrentDictionary<FPackageId, GameFile>();
            return Interlocked.CompareExchange(ref _optionalById, newPackageIndex, null) ?? newPackageIndex;
        }

        /// <summary>
        /// Tries to find the optional segment package ("Foo.o.uasset") cooked for the package
        /// with the given id. Unreal Engine loads and merges it together with the package.
        /// </summary>
        public bool TryGetOptionalSegmentPackage(FPackageId packageId, [MaybeNullWhen(false)] out GameFile file)
        {
            var packageIndex = Volatile.Read(ref _optionalById);
            if (packageIndex is not null && packageIndex.TryGetValue(packageId, out file))
                return true;

            file = null;
            return false;
        }

        public void FindPayloads(GameFile file, out GameFile? uexp, out IReadOnlyList<GameFile> ubulks, out IReadOnlyList<GameFile> uptnls, bool cookedIndexLookup = false)
        {
            uexp = null;
            ubulks = uptnls = new List<GameFile>().AsReadOnly();
            if (!file.IsUePackage) return;

            var ubulkList = new List<GameFile>();
            var uptnlList = new List<GameFile>();

            var path = file.PathWithoutExtension;
            if (cookedIndexLookup && file is FIoStoreEntry { IsUePackage: true } entry)
            {
                // dedicated to FBulkDataCookedIndex payloads but should work fine for anything coming from IoStore
                // hitting IoStore Files like that is quite slow, but it's the only way to get the correct payloads
                // payloads of the package and of its optional segment share the package id, so the multi
                // output index is what tells the two save realms apart (see FIoChunkId)
                foreach (var payload in entry.IoStoreReader.Files.Values.Where(x => x.IsUePackagePayload
                                                                                    && x is FIoStoreEntry y
                                                                                    && y.ChunkId.ChunkId == entry.ChunkId.ChunkId
                                                                                    && y.ChunkId._chunkIndex == entry.ChunkId._chunkIndex))
                {
                    switch (payload.Extension)
                    {
                        case "ubulk":
                            ubulkList.Add(payload);
                            break;
                        case "uptnl":
                            uptnlList.Add(payload);
                            break;
                    }
                }
            }
            else if (file is VfsEntry { Vfs: AbstractVfsReader vfs })
            {
                // file comes from a specific archive
                // this ensure that its payloads are also from the same archive
                // this is useful with patched archives
                vfs.Files.TryGetValue(path + ".uexp", out uexp);
                if (vfs.Files.TryGetValue(path + ".ubulk", out var ubulkVfs))
                    ubulkList.Add(ubulkVfs);
            }

            if (uexp == null) TryGetValue(path + ".uexp", out uexp);
            if (ubulkList.Count < 1 && TryGetValue(path + ".ubulk", out var ubulk))
                ubulkList.Add(ubulk);
            if (uptnlList.Count < 1 && TryGetValue(path + ".uptnl", out var uptnl))
                uptnlList.Add(uptnl);

            ubulks = ubulkList.AsReadOnly();
            uptnls = uptnlList.AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFiles(IReadOnlyDictionary<string, GameFile> newFiles, long readOrder = 0,
            IReadOnlyDictionary<FPackageId, GameFile>? packageFiles = null,
            IReadOnlyDictionary<FPackageId, GameFile>? optionalSegmentPackageFiles = null)
        {
            if (packageFiles is null)
            {
                ConcurrentDictionary<FPackageId, GameFile>? packageIndex = null;
                foreach (var file in newFiles.Values)
                {
                    // packages, their optional variant and their respective payloads share the same id
                    // only load the normal package in this dict for later use by IoPackage.ImportedPackages
                    if (file is FIoStoreEntry { IsPackageData: true, IsOptionalPackage: false } ioEntry)
                    {
                        (packageIndex ??= GetPackageIndex())[ioEntry.ChunkId.AsPackageId()] = file;
                    }
                }
            }
            else
            {
                var packageIndex = GetPackageIndex();
                foreach (var (packageId, file) in packageFiles)
                    packageIndex[packageId] = file;
            }

            _indicesBag.Add(new KeyValuePair<long, IReadOnlyDictionary<string, GameFile>>(readOrder, newFiles));
            Interlocked.Add(ref _count, CountVisible(newFiles));

            if (optionalSegmentPackageFiles is { Count: > 0 })
            {
                var optionalSegmentIndex = GetOptionalSegmentPackageIndex();
                foreach (var (packageId, file) in optionalSegmentPackageFiles)
                    optionalSegmentIndex[packageId] = file;
            }
        }

        /// <summary>
        /// Number of files that are actually listed. Files cooked as part of another file (see
        /// <see cref="GameFile.IsHidden"/>) stay resolvable by path but are not enumerated.
        /// </summary>
        private static int CountVisible(IReadOnlyDictionary<string, GameFile> files)
        {
            return files.Values.Count(file => !file.IsHidden);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _indicesBag.Clear();
            Volatile.Read(ref _byId)?.Clear();
            Volatile.Read(ref _optionalById)?.Clear();
            Interlocked.Exchange(ref _count, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key)
        {
            return _indicesBag.Any(files => files.Value.ContainsKey(key));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out GameFile value)
        {
            foreach (var files in _indicesBag.OrderByDescending(kvp => kvp.Key))
            {
                if (files.Value.TryGetValue(key, out value))
                    return true;
            }

            value = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValues(string key, out List<GameFile> values)
        {
            values = [];
            foreach (var files in _indicesBag.OrderByDescending(kvp => kvp.Key))
            {
                if (files.Value.TryGetValue(key, out var value))
                {
                    values.Add(value);
                }
            }
            return values.Count > 0;
        }

        public GameFile this[string path] => TryGetValue(path, out var value) ? value : throw new KeyNotFoundException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, GameFile>> GetEnumerator()
        {
            return (from index in _indicesBag.OrderByDescending(kvp => kvp.Key) from entry in index.Value where !entry.Value.IsHidden select entry).GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => Volatile.Read(ref _count);

        private class KeyEnumerable : IEnumerable<string>
        {
            private readonly FileProviderDictionary _orig;

            internal KeyEnumerable(FileProviderDictionary orig)
            {
                _orig = orig;
            }

            public IEnumerator<string> GetEnumerator()
            {
                return (from index in _orig._indicesBag.OrderByDescending(kvp => kvp.Key) from entry in index.Value where !entry.Value.IsHidden select entry.Key).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class ValueEnumerable : IEnumerable<GameFile>
        {
            private readonly FileProviderDictionary _orig;

            internal ValueEnumerable(FileProviderDictionary orig)
            {
                _orig = orig;
            }

            public IEnumerator<GameFile> GetEnumerator()
            {
                return (from index in _orig._indicesBag.OrderByDescending(kvp => kvp.Key) from value in index.Value.Values where !value.IsHidden select value).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}