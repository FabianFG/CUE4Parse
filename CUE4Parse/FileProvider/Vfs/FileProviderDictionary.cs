using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.FileProvider.Vfs
{
    public class FileProviderDictionary : IReadOnlyDictionary<string, GameFile>
    {
        private readonly ConcurrentBag<KeyValuePair<long, IReadOnlyDictionary<string, GameFile>>> _indicesBag = new ();

        private readonly ConcurrentDictionary<FPackageId, GameFile> _byId = new ();
        public IReadOnlyDictionary<FPackageId, GameFile> ById => _byId;

        private readonly KeyEnumerable _keys;
        public IEnumerable<string> Keys => _keys;

        private readonly ValueEnumerable _values;
        public IEnumerable<GameFile> Values => _values;

        public FileProviderDictionary()
        {
            _keys = new KeyEnumerable(this);
            _values = new ValueEnumerable(this);
        }

        public void FindPayloads(GameFile file, out GameFile? uexp, out GameFile? ubulk, out GameFile? uptnl)
        {
            uexp = ubulk = uptnl = null;
            if (!file.IsUePackage) return;

            // file comes from a specific archive
            // this ensure that its payloads are also from the same archive
            // this is useful with patched archives

            var path = file.PathWithoutExtension;
            if (file is VfsEntry {Vfs: { } vfs})
            {
                vfs.Files.TryGetValue(path + ".uexp", out uexp);
                vfs.Files.TryGetValue(path + ".ubulk", out ubulk);
            }

            if (uexp == null) TryGetValue(path + ".uexp", out uexp);
            if (ubulk == null) TryGetValue(path + ".ubulk", out ubulk);
            TryGetValue(path + ".uptnl", out uptnl);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFiles(IReadOnlyDictionary<string, GameFile> newFiles, long readOrder = 0)
        {
            foreach (var file in newFiles.Values)
            {
                if (file is FIoStoreEntry ioEntry)
                {
                    _byId[ioEntry.ChunkId.AsPackageId()] = file;
                }
            }
            _indicesBag.Add(new KeyValuePair<long, IReadOnlyDictionary<string, GameFile>>(readOrder, newFiles));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _indicesBag.Clear();
            _byId.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key)
        {
            foreach (var files in _indicesBag)
            {
                if (files.Value.ContainsKey(key))
                    return true;
            }

            return false;
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

        public GameFile this[string path] => TryGetValue(path, out var value) ? value : throw new KeyNotFoundException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, GameFile>> GetEnumerator()
        {
            foreach (var index in _indicesBag.OrderByDescending(kvp => kvp.Key))
            {
                foreach (var entry in index.Value)
                {
                    yield return entry;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _indicesBag.Sum(it => it.Value.Count);

        private class KeyEnumerable : IEnumerable<string>
        {
            private readonly FileProviderDictionary _orig;

            internal KeyEnumerable(FileProviderDictionary orig)
            {
                _orig = orig;
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (var index in _orig._indicesBag.OrderByDescending(kvp => kvp.Key))
                {
                    foreach (var key in index.Value.Keys)
                    {
                        yield return key;
                    }
                }
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
                foreach (var index in _orig._indicesBag.OrderByDescending(kvp => kvp.Key))
                {
                    foreach (var key in index.Value.Values)
                    {
                        yield return key;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
