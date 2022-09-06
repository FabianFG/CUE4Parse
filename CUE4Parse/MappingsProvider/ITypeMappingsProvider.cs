namespace CUE4Parse.MappingsProvider
{
    public interface ITypeMappingsProvider
    {
        public TypeMappings? MappingsForGame { get; }

        public void Load(string path);
        public void Load(byte[] bytes);

        public void Reload();
    }
}
