using System.Collections.Generic;
using System.Threading.Tasks;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public abstract class AbstractTypeMappingsContainer : ITypeMappingsContainer
    {
        public abstract IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>>> TypesByGame { get; protected set; }

        public abstract IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>> EnumsByGame { get; protected set; }


        public TypeMappings ForGame(string game)
        {
            TypesByGame.TryGetValue(game, out var types);
            EnumsByGame.TryGetValue(game, out var enums);
            types ??= new Dictionary<string, IReadOnlyDictionary<int, PropertyInfo>>();
            enums ??= new Dictionary<string, IReadOnlyDictionary<int, string>>();
            return new TypeMappings(types, enums);
        }

        public abstract bool Reload();
        public abstract Task<bool> ReloadAsync();
    }
}