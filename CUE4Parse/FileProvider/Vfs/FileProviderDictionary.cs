using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CUE4Parse.FileProvider.Vfs
{
    public class FileProviderDictionary : IReadOnlyDictionary<string, GameFile>
    {
        private ConcurrentBag<IReadOnlyDictionary<string, GameFile>> _indicesBag = new ConcurrentBag<IReadOnlyDictionary<string, GameFile>>();

        public bool IsCaseInsensitive;

        public FileProviderDictionary(bool isCaseInsensitive)
        {
            IsCaseInsensitive = isCaseInsensitive;
            _keys = new KeyEnumerable(this);
            _values = new ValueEnumerable(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddFiles(IReadOnlyDictionary<string, GameFile> newFiles)
        {
            _indicesBag.Add(newFiles);
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key)
        {
            if (IsCaseInsensitive)
                key = key.ToLowerInvariant();
            foreach (var files in _indicesBag)
            {
                if (files.ContainsKey(key))
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out GameFile value)
        {
            if (IsCaseInsensitive)
                key = key.ToLowerInvariant();
            foreach (var files in _indicesBag)
            {
                if (files.TryGetValue(key, out value))
                    return true;
            }

            value = default;
            return false;
        }

        
        public GameFile this[string path]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                TryGetValue(path, out var file);
                return file ?? throw new KeyNotFoundException($"There is no game file with the path \"{path}\"");
            }
        }


        public int Count => _indicesBag.Sum(it => it.Count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, GameFile>> GetEnumerator()
        {
            foreach (var index in _indicesBag)
            {
                foreach (var entry in index)
                {
                    yield return entry;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private KeyEnumerable _keys;
        private ValueEnumerable _values;
        public IEnumerable<string> Keys => _keys;
        public IEnumerable<GameFile> Values => _values;

        private class KeyEnumerable : IEnumerable<string>
        {
            private FileProviderDictionary _orig;

            internal KeyEnumerable(FileProviderDictionary orig)
            {
                _orig = orig;
            }
            
            public IEnumerator<string> GetEnumerator()
            {
                foreach (var index in _orig._indicesBag)
                {
                    foreach (var key in index.Keys)
                    {
                        yield return key;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        
        private class ValueEnumerable : IEnumerable<GameFile>
        {
            private FileProviderDictionary _orig;

            internal ValueEnumerable(FileProviderDictionary orig)
            {
                _orig = orig;
            }
            
            public IEnumerator<GameFile> GetEnumerator()
            {
                foreach (var index in _orig._indicesBag)
                {
                    foreach (var key in index.Values)
                    {
                        yield return key;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}