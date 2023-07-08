using System.Collections.Generic;

namespace CUE4Parse.MappingsProvider;

public class TypeMappings(Dictionary<string, Struct> types, Dictionary<string, Dictionary<int, string>> enums)
{
    public readonly Dictionary<string, Struct> Types = types;
    public readonly Dictionary<string, Dictionary<int, string>> Enums = enums;

    public TypeMappings() : this(new Dictionary<string, Struct>(), new Dictionary<string, Dictionary<int, string>>()) { }
}
