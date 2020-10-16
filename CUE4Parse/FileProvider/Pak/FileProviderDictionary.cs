using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CUE4Parse.FileProvider.Pak
{
    public class FileProviderDictionary : IReadOnlyDictionary<string, GameFile>
    {
        private ConcurrentBag<IReadOnlyDictionary<string, GameFile>> _indicesBag = new ConcurrentBag<IReadOnlyDictionary<string, GameFile>>();

        public bool IsCaseInsensitive;

        public FileProviderDictionary(bool isCaseInsensitive)
        {
            IsCaseInsensitive = isCaseInsensitive;
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

        public IEnumerable<string> Keys => throw new InvalidOperationException();
        public IEnumerable<GameFile> Values => throw new InvalidOperationException();

        
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
    }
}