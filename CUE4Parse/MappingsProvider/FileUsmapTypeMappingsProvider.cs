using System;
using System.IO;

namespace CUE4Parse.MappingsProvider
{
    public sealed class FileUsmapTypeMappingsProvider : UsmapTypeMappingsProvider
    {
        private readonly StringComparer _stringComparer;
        private readonly string _path;
        public readonly string FileName;

        public FileUsmapTypeMappingsProvider(string path, StringComparer? comparer = null)
        {
            _stringComparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            _path = path;
            FileName = Path.GetFileName(_path);
            Load(path, _stringComparer);
        }

        public override void Reload()
        {
            Load(_path, _stringComparer);
        }
    }
}
