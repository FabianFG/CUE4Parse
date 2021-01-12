using System.Collections.Generic;
using System.Threading.Tasks;

namespace CUE4Parse.MappingsProvider
{
    public interface ITypeMappingsProvider
    {
        public Dictionary<string, TypeMappings> MappingsByGame { get; }

        public TypeMappings ForGame(string game);

        public bool Reload();
        public Task<bool> ReloadAsync();
    }
}