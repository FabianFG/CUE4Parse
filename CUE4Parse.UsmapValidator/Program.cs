using CUE4Parse.MappingsProvider;
using CUE4Parse.MappingsProvider.Usmap;

var listCollisions = args.Length == 2 && args[0] == "--list-collisions";
if (args.Length != 1 && !listCollisions)
{
    Console.Error.WriteLine("usage: usmap_validate [--list-collisions] <mapping.usmap>");
    return 2;
}

try
{
    var parser = new UsmapParser(File.ReadAllBytes(args[^1]));
    if (parser.Mappings is not TypeMappings mappings)
        throw new InvalidDataException("USMAP parser did not return TypeMappings");
    var duplicateTypeLeafGroups = mappings.Types.Keys
        .GroupBy(TypeMappings.GetShortTypeName, StringComparer.OrdinalIgnoreCase)
        .Where(group => group.Skip(1).Any())
        .ToArray();
    var duplicateEnumLeafGroups = mappings.Enums.Keys
        .GroupBy(TypeMappings.GetShortTypeName, StringComparer.OrdinalIgnoreCase)
        .Where(group => group.Skip(1).Any())
        .ToArray();

    foreach (var (identifier, expected) in mappings.Types)
    {
        if (!mappings.TryGetType(TypeMappings.GetShortTypeName(identifier), identifier, out var actual) ||
            !ReferenceEquals(expected, actual))
            throw new InvalidDataException($"Exact type lookup did not round-trip {identifier}");
    }
    foreach (var (identifier, expected) in mappings.Enums)
    {
        if (!mappings.TryGetEnum(TypeMappings.GetShortTypeName(identifier), identifier, out var actual) ||
            !ReferenceEquals(expected, actual))
            throw new InvalidDataException($"Exact enum lookup did not round-trip {identifier}");
    }
    if (mappings.UsesFullTypeIdentifiers &&
        (mappings.Types.Keys.Any(identifier =>
             mappings.TryGetType(TypeMappings.GetShortTypeName(identifier), null, out _)) ||
         mappings.Enums.Keys.Any(identifier =>
             mappings.TryGetEnum(TypeMappings.GetShortTypeName(identifier), null, out _))))
        throw new InvalidDataException("A short-only lookup succeeded in full-identifier mode");

    Console.WriteLine(
        $"enums={mappings.Enums.Count} types={mappings.Types.Count} " +
        $"fullIdentifiers={mappings.UsesFullTypeIdentifiers.ToString().ToLowerInvariant()} " +
        $"exactTypeLookups={mappings.Types.Count} exactEnumLookups={mappings.Enums.Count} " +
        $"duplicateTypeLeaves={duplicateTypeLeafGroups.Length} duplicateEnumLeaves={duplicateEnumLeafGroups.Length}");
    if (listCollisions)
    {
        foreach (var group in duplicateTypeLeafGroups)
            Console.WriteLine($"type-leaf {group.Key}: {string.Join(", ", group)}");
        foreach (var group in duplicateEnumLeafGroups)
            Console.WriteLine($"enum-leaf {group.Key}: {string.Join(", ", group)}");
    }
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
