using System.Collections.Generic;
using System.Threading.Tasks;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public interface ITypeMappingsProvider
    {
        public Dictionary<string, TypeMappings> MappingsByGame { get; }

        public TypeMappings ForGame(string game);

        public bool Reload();
        public Task<bool> ReloadAsync();
    }
}