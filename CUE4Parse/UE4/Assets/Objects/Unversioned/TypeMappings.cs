using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public class TypeMappings
    {
        public readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>> Types;
        public readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> Enums;

        public TypeMappings(IReadOnlyDictionary<string, IReadOnlyDictionary<int, PropertyInfo>> types, IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> enums)
        {
            Types = types;
            Enums = enums;
        }
    }
}