using System.Collections.Generic;
using System.Threading.Tasks;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public interface ITypeMappingsContainer
    {
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>>> TypesByGame { get; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>> EnumsByGame { get; }

        public TypeMappings ForGame(string game);

        public bool Reload();
        public Task<bool> ReloadAsync();
    }
}