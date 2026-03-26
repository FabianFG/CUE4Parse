using System.Collections.Generic;

namespace CUE4Parse.MappingsProvider
{
    public class TypeMappings
    {
        public readonly Dictionary<string, Struct> Types;
        public readonly Dictionary<string, Dictionary<long, string>> Enums;

        public TypeMappings(Dictionary<string, Struct> types, Dictionary<string, Dictionary<long, string>> enums)
        {
            Types = types;
            Enums = enums;
        }

        public TypeMappings()
        {
            Types = new Dictionary<string, Struct>();
            Enums = new Dictionary<string, Dictionary<long, string>>();
        }
    }
}
