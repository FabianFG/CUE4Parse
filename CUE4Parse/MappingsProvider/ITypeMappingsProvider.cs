using System;

namespace CUE4Parse.MappingsProvider
{
    public interface ITypeMappingsProvider
    {
        public TypeMappings? MappingsForGame { get; }

        public void Load(string path, StringComparer? comparer = null);
        public void Load(byte[] bytes, StringComparer? comparer = null);

        public void Reload();
    }
}
