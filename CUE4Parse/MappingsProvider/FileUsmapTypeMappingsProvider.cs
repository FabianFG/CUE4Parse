namespace CUE4Parse.MappingsProvider
{
    public sealed class FileUsmapTypeMappingsProvider : UsmapTypeMappingsProvider
    {
        private readonly string _path;

        public FileUsmapTypeMappingsProvider(string path)
        {
            _path = path;
            Load(path);
        }

        public override void Reload()
        {
            Load(_path);
        }
    }
}
