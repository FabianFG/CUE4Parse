namespace CUE4Parse.MappingsProvider
{
    public abstract class AbstractTypeMappingsProvider : ITypeMappingsProvider
    {
        public abstract TypeMappings? MappingsForGame { get; protected set; }

        public abstract void Load(string path);
        public abstract void Load(byte[] bytes);

        public abstract void Reload();
    }
}
