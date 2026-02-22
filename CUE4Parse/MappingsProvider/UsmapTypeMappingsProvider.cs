using CUE4Parse.MappingsProvider.Usmap;
using System;

namespace CUE4Parse.MappingsProvider
{
    public abstract class UsmapTypeMappingsProvider : AbstractTypeMappingsProvider
    {
        public override TypeMappings? MappingsForGame { get; protected set; } = new();

        public override void Load(string path, StringComparer? comparer = null)
        {
            var usmap = new UsmapParser(path, comparer: comparer);
            MappingsForGame = usmap.Mappings;
        }

        public override void Load(byte[] bytes, StringComparer? comparer = null)
        {
            var usmap = new UsmapParser(bytes, comparer: comparer);
            MappingsForGame = usmap.Mappings;
        }
    }
}