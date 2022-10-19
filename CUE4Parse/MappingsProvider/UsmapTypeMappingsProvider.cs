using CUE4Parse.MappingsProvider.Usmap;

namespace CUE4Parse.MappingsProvider
{
    public abstract class UsmapTypeMappingsProvider : AbstractTypeMappingsProvider
    {
        public override TypeMappings? MappingsForGame { get; protected set; } = new();

        public override void Load(string path)
        {
            var usmap = new UsmapParser(path);
            MappingsForGame = usmap.Mappings;
        }

        public override void Load(byte[] bytes)
        {
            var usmap = new UsmapParser(bytes);
            MappingsForGame = usmap.Mappings;
        }
    }
}