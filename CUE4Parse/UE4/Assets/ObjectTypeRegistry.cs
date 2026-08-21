using System.Reflection;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse.UE4.Assets;

public sealed class SkipObjectRegistrationAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ObjectTypeAttribute(string fullIdentifier) : Attribute
{
    public string FullIdentifier { get; } = fullIdentifier;
}

public static class ObjectTypeRegistry
{
    private static readonly Type _propertyHolderType = typeof(IPropertyHolder);
    private static readonly Dictionary<string, Type> _classes =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Type> _classesByFullIdentifier =
        new(StringComparer.OrdinalIgnoreCase);

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
            foreach (var objectType in definedType.GetCustomAttributes<ObjectTypeAttribute>(false))
                RegisterClass(objectType.FullIdentifier, definedType);
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
            if (TypeMappings.IsFullTypeIdentifier(serializedName))
            {
                if (_classesByFullIdentifier.TryGetValue(serializedName, out var registered) && registered != type)
                    throw new ArgumentException(
                        $"Object parser {serializedName} is already registered as {registered.FullName}",
                        nameof(serializedName));
                _classesByFullIdentifier[serializedName] = type;
            }
            else
            {
                _classes[serializedName] = type;
            }
        }
    }

    public static Type? GetClass(string serializedName, string? fullIdentifier = null,
        TypeMappings? mappings = null)
    {
        lock (_classes)
        {
            if (!TypeMappings.IsFullTypeIdentifier(fullIdentifier) &&
                mappings?.TryResolveUniqueTypeIdentifier(serializedName, out var resolvedIdentifier) == true)
                fullIdentifier = resolvedIdentifier;

            if (mappings?.UsesFullTypeIdentifiers == true &&
                !TypeMappings.IsFullTypeIdentifier(fullIdentifier))
                return null;

            if (TypeMappings.IsFullTypeIdentifier(fullIdentifier))
            {
                if (!TypeMappings.GetShortTypeName(fullIdentifier!).Equals(
                        serializedName, StringComparison.OrdinalIgnoreCase))
                    return null;

                if (_classesByFullIdentifier.TryGetValue(fullIdentifier!, out var exactType))
                    return exactType;

                // Complete identity is authoritative. A unique schema leaf
                // does not prove that an arbitrary C# parser registered under
                // that leaf owns this UObject path.
                if (mappings?.UsesFullTypeIdentifiers == true)
                    return null;
            }

            if (!_classes.TryGetValue(serializedName, out var type) &&
                mappings?.UsesFullTypeIdentifiers != true && serializedName.EndsWith("_C"))
            {
                _classes.TryGetValue(serializedName[..^2], out type);
            }

            return type;
        }
    }

    public static Type? Get(string serializedName, string? fullIdentifier = null,
        TypeMappings? mappings = null)
    {
        return GetClass(serializedName, fullIdentifier, mappings);
        // TODO add script structs
    }
}
