using System;
using System.Collections.Generic;
using System.Reflection;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse.UE4.Assets;

public sealed class SkipObjectRegistrationAttribute : Attribute;

public static class ObjectTypeRegistry
{
    private static readonly Type _propertyHolderType = typeof(IPropertyHolder);
    private static readonly Dictionary<string, Type> _classes = [];

    static ObjectTypeRegistry()
    {
        RegisterEngine(_propertyHolderType.Assembly);
    }

    public static void RegisterEngine(Assembly assembly)
    {
        var skipAttributeType = typeof(SkipObjectRegistrationAttribute);

        foreach (var definedType in assembly.DefinedTypes)
        {
            if (definedType.IsAbstract ||
                definedType.IsInterface ||
                !_propertyHolderType.IsAssignableFrom(definedType))
            {
                continue;
            }

            if (definedType.GetCustomAttributes(skipAttributeType, false).Length != 0)
            {
                continue;
            }

            RegisterClass(definedType);
        }
    }

    public static void RegisterClass(Type type)
    {
        var name = type.Name;
        if ((name[0] == 'U' || name[0] == 'A') && char.IsUpper(name[1]))
            name = name[1..];
        RegisterClass(name, type);
    }

    public static void RegisterClass(string serializedName, Type type)
    {
        lock (_classes)
        {
            _classes[serializedName] = type;
        }
    }

    public static Type? GetClass(string serializedName)
    {
        lock (_classes)
        {
            if (!_classes.TryGetValue(serializedName, out var type) && serializedName.EndsWith("_C"))
            {
                _classes.TryGetValue(serializedName[..^2], out type);
            }
            return type;
        }
    }

    public static Type? Get(string serializedName)
    {
        return GetClass(serializedName);
        // TODO add script structs
    }
}
