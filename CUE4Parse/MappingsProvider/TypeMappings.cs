namespace CUE4Parse.MappingsProvider;

public class TypeMappings
{
    // These dictionaries expose the serialized mapping model for enumeration
    // and provider population. Runtime lookup must go through TryGetType /
    // TryGetEnum so a full identifier is never collapsed to an ambiguous leaf.
    public readonly TrackedDictionary<string, Struct> Types;
    public readonly TrackedDictionary<string, Dictionary<long, string>> Enums;
    private bool? _validatedFullTypeIdentifiers;
    private Dictionary<string, string>? _typeKeysByFullIdentifier;
    private Dictionary<string, string>? _enumKeysByFullIdentifier;
    private Dictionary<string, string?>? _typeIdentifiersByShortName;
    private Dictionary<string, string?>? _enumIdentifiersByShortName;
    private ulong _mutationVersion;
    private ulong _validatedMutationVersion = ulong.MaxValue;

    /// <summary>
    /// True when this mapping uses complete Unreal object paths as type keys,
    /// for example <c>/Script/CoreUObject.Object</c> or
    /// <c>/Game/Foo/Bar.Bar_C</c>.  Standard mappings use the leaf FName only.
    /// </summary>
    public bool UsesFullTypeIdentifiers
    {
        get
        {
            EnsureValidated();
            return _validatedFullTypeIdentifiers == true;
        }
    }

    public TypeMappings(Dictionary<string, Struct> types, Dictionary<string, Dictionary<long, string>> enums)
    {
        Types = new TrackedDictionary<string, Struct>(types, types.Comparer, Invalidate,
            type => type.AttachChangeTracker(this));
        Enums = new TrackedDictionary<string, Dictionary<long, string>>(enums, enums.Comparer, Invalidate);
    }

    public TypeMappings()
    {
        Types = new TrackedDictionary<string, Struct>(StringComparer.OrdinalIgnoreCase, Invalidate,
            type => type.AttachChangeTracker(this));
        Enums = new TrackedDictionary<string, Dictionary<long, string>>(StringComparer.OrdinalIgnoreCase, Invalidate);
    }

    /// <summary>
    /// Resolve a schema without collapsing distinct UObjects which share the
    /// same leaf FName. Full-identifier mappings require the caller to retain
    /// and provide the complete identity; short-only lookup fails closed.
    /// </summary>
    public bool TryGetType(string shortName, string? fullIdentifier, out Struct type)
    {
        EnsureValidated();
        if (_validatedFullTypeIdentifiers == true)
        {
            var identifier = IsFullTypeIdentifier(fullIdentifier)
                ? fullIdentifier
                : IsFullTypeIdentifier(shortName) ? shortName : null;
            if (identifier != null && IdentifierMatchesLookupName(shortName, identifier))
                return TryGetFullType(identifier, out type!);

            type = null!;
            return false;
        }

        var legacyName = IsFullTypeIdentifier(fullIdentifier)
            ? GetShortTypeName(fullIdentifier!)
            : GetShortTypeName(shortName);
        return Types.TryGetValue(legacyName, out type!);
    }

    public bool TryGetEnum(string shortName, string? fullIdentifier, out Dictionary<long, string> values)
    {
        EnsureValidated();
        if (_validatedFullTypeIdentifiers == true)
        {
            var identifier = IsFullTypeIdentifier(fullIdentifier)
                ? fullIdentifier
                : IsFullTypeIdentifier(shortName) ? shortName : null;
            if (identifier != null && IdentifierMatchesLookupName(shortName, identifier))
                return TryGetFullEnum(identifier, out values!);

            values = null!;
            return false;
        }

        var legacyName = IsFullTypeIdentifier(fullIdentifier)
            ? GetShortTypeName(fullIdentifier!)
            : GetShortTypeName(shortName);
        return Enums.TryGetValue(legacyName, out values!);
    }

    /// <summary>
    /// True only when <paramref name="fullIdentifier"/> is the sole type in
    /// this mapping with its leaf FName. This permits legacy leaf-based
    /// extension points without reintroducing ambiguous last-wins behavior.
    /// </summary>
    public bool IsTypeIdentifierLeafUnique(string fullIdentifier)
    {
        EnsureValidated();
        if (_validatedFullTypeIdentifiers != true || !IsFullTypeIdentifier(fullIdentifier))
            return false;

        EnsureShortNameIndexes();
        return _typeIdentifiersByShortName!.TryGetValue(GetShortTypeName(fullIdentifier), out var uniqueIdentifier) &&
               uniqueIdentifier != null &&
               uniqueIdentifier.Equals(fullIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Upgrade a leaf-only type name from a wire format which cannot carry an
    /// owner to the sole complete identifier in this mapping. Ambiguous or
    /// absent leaves fail instead of selecting an arbitrary schema.
    /// </summary>
    public bool TryResolveUniqueTypeIdentifier(string name, out string identifier)
    {
        EnsureValidated();
        if (_validatedFullTypeIdentifiers == true)
        {
            if (IsFullTypeIdentifier(name))
                return _typeKeysByFullIdentifier!.TryGetValue(name, out identifier!);

            EnsureShortNameIndexes();
            if (_typeIdentifiersByShortName!.TryGetValue(GetShortTypeName(name), out var uniqueIdentifier) &&
                uniqueIdentifier != null)
            {
                if (TryGetFullType(uniqueIdentifier, out _))
                {
                    identifier = uniqueIdentifier;
                    return true;
                }
            }
        }

        identifier = null!;
        return false;
    }

    /// <summary>
    /// Enum counterpart of <see cref="TryResolveUniqueTypeIdentifier"/>.
    /// </summary>
    public bool TryResolveUniqueEnumIdentifier(string name, out string identifier)
    {
        EnsureValidated();
        if (_validatedFullTypeIdentifiers == true)
        {
            if (IsFullTypeIdentifier(name))
                return _enumKeysByFullIdentifier!.TryGetValue(name, out identifier!);

            EnsureShortNameIndexes();
            if (_enumIdentifiersByShortName!.TryGetValue(GetShortTypeName(name), out var uniqueIdentifier) &&
                uniqueIdentifier != null)
            {
                if (TryGetFullEnum(uniqueIdentifier, out _))
                {
                    identifier = uniqueIdentifier;
                    return true;
                }
            }
        }

        identifier = null!;
        return false;
    }

    /// <summary>
    /// Compare a complete identifier against a named game-specific handler
    /// without discarding the identifier. Long mappings first resolve the
    /// handler name to its sole complete owner and then compare exact paths.
    /// </summary>
    public bool MatchesResolvedTypeIdentifier(string identifier, string handlerName)
    {
        EnsureValidated();
        if (_validatedFullTypeIdentifiers == true)
            return IsFullTypeIdentifier(identifier) &&
                   TryResolveUniqueTypeIdentifier(handlerName, out var handlerIdentifier) &&
                   identifier.Equals(handlerIdentifier, StringComparison.OrdinalIgnoreCase);

        return GetShortTypeName(identifier).Equals(handlerName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFullTypeIdentifier(string? value) =>
        !string.IsNullOrEmpty(value) && value[0] == '/' && value.Contains('.');

    public static string GetShortTypeName(string identifier)
    {
        var separator = Math.Max(identifier.LastIndexOf('.'), identifier.LastIndexOf(':'));
        return separator >= 0 ? identifier[(separator + 1)..] : identifier;
    }

    /// <summary>
    /// Reconstruct the complete object identifier carried by UE's complete
    /// property type name. Script modules may be serialized either as the
    /// package name (<c>/Script/CoreUObject</c>) or as its short module name.
    /// </summary>
    public static string? QualifyTypeIdentifier(string? identifier, string? module)
    {
        if (string.IsNullOrEmpty(identifier) || IsFullTypeIdentifier(identifier) ||
            string.IsNullOrEmpty(module) || module.Equals("None", StringComparison.OrdinalIgnoreCase))
            return identifier;

        var owner = module[0] == '/' ? module : "/Script/" + module;
        return IsFullTypeIdentifier(owner)
            ? $"{owner}:{identifier}"
            : $"{owner}.{identifier}";
    }

    /// <summary>
    /// Reject mixed or partially-qualified files.  Such a file cannot be
    /// resolved deterministically because some entries would still collide by
    /// leaf FName.
    /// </summary>
    public void ValidateIdentifierMode()
    {
        foreach (var type in Types.Values)
            type.ResetSuperResolution();

        var hasFullIdentifiers = DetectFullTypeIdentifiers();
        var hasShortIdentifiers = Types.Keys.Any(x => !IsFullTypeIdentifier(x)) ||
                                  Enums.Keys.Any(x => !IsFullTypeIdentifier(x));
        if (hasFullIdentifiers && hasShortIdentifiers)
            throw new ArgumentException("USMAP mixes full and short type/enum identifiers");

        if (!hasFullIdentifiers)
        {
            _validatedFullTypeIdentifiers = false;
            _typeKeysByFullIdentifier = null;
            _enumKeysByFullIdentifier = null;
            _typeIdentifiersByShortName = null;
            _enumIdentifiersByShortName = null;
            CaptureMutationVersion();
            return;
        }

        _typeKeysByFullIdentifier = BuildFullIdentifierIndex(Types, "type");
        _enumKeysByFullIdentifier = BuildFullIdentifierIndex(Enums, "enum");

        var crossKindIdentifier = _typeKeysByFullIdentifier.Keys.FirstOrDefault(_enumKeysByFullIdentifier.ContainsKey);
        if (crossKindIdentifier != null)
            throw new ArgumentException($"Full-identifier mapping describes {crossKindIdentifier} as both a type and an enum");

        foreach (var identifier in _typeKeysByFullIdentifier.Keys)
        {
            var type = Types[_typeKeysByFullIdentifier[identifier]];
            if (!identifier.Equals(type.Name, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Full-identifier USMAP key {identifier} contains schema named {type.Name}");
            if (type.SuperType is { } superType)
            {
                if (!IsFullTypeIdentifier(superType))
                    throw new ArgumentException($"Full-identifier USMAP type {identifier} has short super {superType}");
                if (!_typeKeysByFullIdentifier.ContainsKey(superType))
                    throw new ArgumentException($"Full-identifier USMAP type {identifier} has missing super {superType}");
            }
            ValidatePropertyIdentifiers(identifier, type.Properties.Values.Select(x => x.MappingType));
        }

        ValidateInheritanceGraph();
        BuildShortNameIndexes();
        _validatedFullTypeIdentifiers = true;
        CaptureMutationVersion();
    }

    private bool DetectFullTypeIdentifiers() =>
        Types.Keys.Any(IsFullTypeIdentifier) || Enums.Keys.Any(IsFullTypeIdentifier);

    private void EnsureValidated()
    {
        if (_validatedFullTypeIdentifiers == null || _validatedMutationVersion != _mutationVersion)
            ValidateIdentifierMode();
    }

    private bool TryGetFullType(string identifier, out Struct type)
    {
        if (_typeKeysByFullIdentifier!.TryGetValue(identifier, out var storedKey) &&
            Types.TryGetValue(storedKey, out type!))
            return true;

        type = null!;
        return false;
    }

    private bool TryGetFullEnum(string identifier, out Dictionary<long, string> values)
    {
        if (_enumKeysByFullIdentifier!.TryGetValue(identifier, out var storedKey) &&
            Enums.TryGetValue(storedKey, out values!))
            return true;

        values = null!;
        return false;
    }

    internal void Invalidate()
    {
        unchecked
        {
            _mutationVersion++;
        }
    }

    private void CaptureMutationVersion() => _validatedMutationVersion = _mutationVersion;

    private static bool IdentifierMatchesLookupName(string name, string identifier) =>
        IsFullTypeIdentifier(name)
            ? name.Equals(identifier, StringComparison.OrdinalIgnoreCase)
            : GetShortTypeName(name).Equals(GetShortTypeName(identifier), StringComparison.OrdinalIgnoreCase);

    private void ValidatePropertyIdentifiers(string owner, IEnumerable<PropertyType> properties)
    {
        foreach (var property in properties)
        {
            if (property.StructType is { } structType)
            {
                if (!IsFullTypeIdentifier(structType))
                    throw new ArgumentException($"Full-identifier USMAP type {owner} references short struct {structType}");
                if (!_typeKeysByFullIdentifier!.ContainsKey(structType))
                    throw new ArgumentException($"Full-identifier USMAP type {owner} references missing struct {structType}");
            }
            if (property.EnumName is { } enumName)
            {
                if (!IsFullTypeIdentifier(enumName))
                    throw new ArgumentException($"Full-identifier USMAP type {owner} references short enum {enumName}");
                if (!_enumKeysByFullIdentifier!.ContainsKey(enumName))
                    throw new ArgumentException($"Full-identifier USMAP type {owner} references missing enum {enumName}");
            }
            if (property.InnerType != null)
                ValidatePropertyIdentifiers(owner, [property.InnerType]);
            if (property.ValueType != null)
                ValidatePropertyIdentifiers(owner, [property.ValueType]);
        }
    }

    private void ValidateInheritanceGraph()
    {
        var types = _typeKeysByFullIdentifier!;
        var states = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        foreach (var start in types.Keys)
        {
            if (states.TryGetValue(start, out var completed) && completed == 2)
                continue;

            var path = new List<string>();
            var identifier = start;
            while (true)
            {
                if (states.TryGetValue(identifier, out var state))
                {
                    if (state == 1)
                        throw new ArgumentException($"Full-identifier USMAP inheritance cycle contains {identifier}");
                    break;
                }

                states[identifier] = 1;
                path.Add(identifier);
                if (Types[types[identifier]].SuperType is not { } superType)
                    break;
                identifier = superType;
            }

            foreach (var visited in path)
                states[visited] = 2;
        }
    }

    private void EnsureShortNameIndexes()
    {
        if (_typeIdentifiersByShortName == null || _enumIdentifiersByShortName == null)
            BuildShortNameIndexes();
    }

    private void BuildShortNameIndexes()
    {
        _typeIdentifiersByShortName = BuildShortNameIndex(_typeKeysByFullIdentifier!.Keys);
        _enumIdentifiersByShortName = BuildShortNameIndex(_enumKeysByFullIdentifier!.Keys);
    }

    private static Dictionary<string, string> BuildFullIdentifierIndex<TValue>(
        IEnumerable<KeyValuePair<string, TValue>> entries, string kind)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (identifier, _) in entries)
        {
            if (!result.TryAdd(identifier, identifier))
                throw new ArgumentException($"Full-identifier mapping contains duplicate {kind} {identifier} under FName case semantics");
        }
        return result;
    }

    private static Dictionary<string, string?> BuildShortNameIndex(IEnumerable<string> identifiers)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var identifier in identifiers)
        {
            var shortName = GetShortTypeName(identifier);
            if (!result.TryAdd(shortName, identifier))
                result[shortName] = null;
        }
        return result;
    }
}
