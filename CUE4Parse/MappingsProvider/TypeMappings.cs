using System.Collections.Generic;

namespace CUE4Parse.MappingsProvider
{
    public class TypeMappings
    {
        public readonly Dictionary<string, Struct> Types;
        public readonly Dictionary<string, List<(long, string)>> Enums;

        public TypeMappings()
        {
            Types = new Dictionary<string, Struct>();
            Enums = new Dictionary<string, List<(long, string)>>();
        }
    }
}
