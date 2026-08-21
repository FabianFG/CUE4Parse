using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Assets.Readers;

public static class FAssetArchiveMappingExtensions
{
    /// <summary>
    /// Resolve a schema name at the wire boundary. Full-identifier mappings
    /// must produce one complete UObject path; ambiguous leaves fail closed.
    /// </summary>
    public static string ResolveTypeIdentifier(this FAssetArchive archive, string identifier)
    {
        var mappings = archive.Owner?.Mappings;
        if (mappings?.UsesFullTypeIdentifiers != true || TypeMappings.IsFullTypeIdentifier(identifier))
            return identifier;

        if (mappings.TryResolveUniqueTypeIdentifier(identifier, out var resolvedIdentifier))
            return resolvedIdentifier;

        throw new ParserException(archive,
            $"Full-identifier mappings cannot resolve the schema leaf {identifier} uniquely");
    }

    /// <summary>
    /// Enum counterpart of <see cref="ResolveTypeIdentifier"/>. A long
    /// mapping must never silently turn an ambiguous enum leaf into a numeric
    /// fallback value.
    /// </summary>
    public static string ResolveEnumIdentifier(this FAssetArchive archive, string identifier)
    {
        var mappings = archive.Owner?.Mappings;
        if (mappings?.UsesFullTypeIdentifiers != true || TypeMappings.IsFullTypeIdentifier(identifier))
            return identifier;

        if (mappings.TryResolveUniqueEnumIdentifier(identifier, out var resolvedIdentifier))
            return resolvedIdentifier;

        throw new ParserException(archive,
            $"Full-identifier mappings cannot resolve the enum leaf {identifier} uniquely");
    }

    public static string ResolveEnumIdentifier(this FKismetArchive archive, string identifier)
    {
        var mappings = archive.Owner.Mappings;
        if (mappings?.UsesFullTypeIdentifiers != true || TypeMappings.IsFullTypeIdentifier(identifier))
            return identifier;

        if (mappings.TryResolveUniqueEnumIdentifier(identifier, out var resolvedIdentifier))
            return resolvedIdentifier;

        throw new ParserException(archive,
            $"Full-identifier mappings cannot resolve the enum leaf {identifier} uniquely");
    }
}
