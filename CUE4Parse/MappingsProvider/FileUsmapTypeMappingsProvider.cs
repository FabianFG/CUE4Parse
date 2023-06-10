using System.IO;

namespace CUE4Parse.MappingsProvider
{
    public sealed class FileUsmapTypeMappingsProvider : UsmapTypeMappingsProvider
    {
        private readonly string _path;
        public readonly string FileName;

        public FileUsmapTypeMappingsProvider(string path)
        {
            _path = path;
            FileName = Path.GetFileName(_path);
            Load(path);
        }

        public override void Reload()
        {
            Load(_path);
        }
    }
}
