using System;

namespace CUE4Parse.MappingsProvider
{
    public abstract class AbstractTypeMappingsProvider : ITypeMappingsProvider
    {
        public abstract TypeMappings? MappingsForGame { get; protected set; }

        public abstract void Load(string path, StringComparer? comparer = null);
        public abstract void Load(byte[] bytes, StringComparer? comparer = null);

        public abstract void Reload();
    }
}
