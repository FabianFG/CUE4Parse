using System.Collections.Generic;
using System.Threading.Tasks;

namespace CUE4Parse.MappingsProvider
{
    public abstract class AbstractTypeMappingsProvider : ITypeMappingsProvider
    {
        public abstract Dictionary<string, TypeMappings> MappingsByGame { get; protected set; }
        
        public TypeMappings ForGame(string game)
        {
            return MappingsByGame.TryGetValue(game, out var mappings) ? mappings : new TypeMappings();
        }

        public abstract bool Reload();
        public abstract Task<bool> ReloadAsync();
    }
}