using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Localization;
using UE4Config.Parsing;

namespace CUE4Parse.FileProvider;

public class InternationalizationDictionary : IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
{
    private readonly IEqualityComparer<string>? _comparer;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _collection = new();

    private readonly List<string> _availableCultures = new();
    public IReadOnlyList<string> AvailableCultures => _availableCultures;

    private readonly Dictionary<string, string> _cultureMappings;
    public IReadOnlyDictionary<string, string> CultureMappings => _cultureMappings;

    private readonly List<string> _localizationPaths = new();
    public IReadOnlyList<string> LocalizationPaths => _localizationPaths;

    public string? Culture { get; private set; }

    internal InternationalizationDictionary(IEqualityComparer<string>? comparer)
    {
        _comparer = comparer;
        _cultureMappings = new Dictionary<string, string>(_comparer);
    }

    internal void InitFromIni(CustomConfigIni ini)
    {
        _availableCultures.Clear();
        _cultureMappings.Clear();
        _localizationPaths.Clear();

        var instructions = new List<InstructionToken>();
        ini.FindPropertyInstructions("/Script/UnrealEd.ProjectPackagingSettings", "CulturesToStage", instructions);
        foreach (var instruction in instructions.Where(x => x.InstructionType == InstructionType.Add))
        {
            _availableCultures.Add(instruction.Value);
        }

        instructions.Clear();
        ini.FindPropertyInstructions("Internationalization", "CultureMappings", instructions);
        foreach (var instruction in instructions.Where(x => x.InstructionType == InstructionType.Add))
        {
            var parts = instruction.Value.Trim('"').Split(';');
            _cultureMappings.Add(parts[0], parts[1]);
        }

        instructions.Clear();
        ini.FindPropertyInstructions("Internationalization", "LocalizationPaths", instructions);
        foreach (var instruction in instructions.Where(x => x.InstructionType == InstructionType.Add))
        {
            _localizationPaths.Add(instruction.Value);
        }
    }

    internal void InitFromMeta(FTextLocalizationMetaDataResource meta)
    {
        if (meta.CompiledCultures is null) return;
        _availableCultures.AddRange(meta.CompiledCultures);
    }

    internal void ChangeCulture(string culture, IReadOnlyDictionary<string, GameFile> files)
    {
        if (!TryGetCulture(culture, out var validated))
            throw new KeyNotFoundException($"'{culture}' is not a valid culture.");

        Culture = validated;
        Clear();

        const string exclusion = "(?!Engine).+/";
        // if (_localizationPaths.Count > 0)
        // {
        //     foreach (var localizationPath in _localizationPaths)
        //     {
        //         LoadByPattern($"^{localizationPath.Replace("%GAMEDIR%", exclusion)}/{Culture}/.+.locres$", files);
        //     }
        // }
        // else
        {
            LoadByPattern($"^{exclusion}.+/{Culture}/.+.locres$", files);
        }
    }

    internal bool TryGetCulture(string culture, [MaybeNullWhen(false)] out string validated)
    {
        validated = null;
        if (AvailableCultures.Contains(culture, _comparer))
            validated = culture;
        if (CultureMappings.TryGetValue(culture, out var actualCulture) && AvailableCultures.Contains(actualCulture, _comparer))
            validated = actualCulture;

        return validated != null;
    }

    public string SafeGet(string @namespace, string key, string? defaultValue = null)
    {
        if (TryGetValue(@namespace, out var n) && n.TryGetValue(key, out var value))
        {
            return value;
        }
        return defaultValue ?? string.Empty;
    }

    private void LoadByPattern(string pattern, IReadOnlyDictionary<string, GameFile> files)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Parallel.ForEach(files.Where(x => regex.IsMatch(x.Key)), file =>
        {
            if (!file.Value.TryCreateReader(out var archive)) return;

            var locres = new FTextLocalizationResource(archive);
            foreach (var entries in locres.Entries)
            {
                var dictionary = (Dictionary<string, string>) _collection.GetOrAdd(entries.Key.Str, _ => new Dictionary<string, string>());
                lock (dictionary)
                {
                    foreach (var entry in entries.Value)
                    {
                        // TODO: we ignore the value priority here
                        dictionary[entry.Key.Str] = entry.Value.LocalizedString;
                    }
                }
            }
        });
    }

    public void Override(IDictionary<string, IDictionary<string, string>> dictionary)
    {
        foreach (var entries in dictionary)
        {
            var d = (Dictionary<string, string>) _collection.GetOrAdd(entries.Key, _ => new Dictionary<string, string>());
            lock (d)
            {
                foreach (var entry in entries.Value)
                {
                    d[entry.Key] = entry.Value;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _collection.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<KeyValuePair<string, IReadOnlyDictionary<string, string>>> GetEnumerator() => _collection.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _collection.Sum(x => x.Value.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(string @namespace) => _collection.ContainsKey(@namespace);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(string @namespace, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, string> value) => _collection.TryGetValue(@namespace, out value);

    public IReadOnlyDictionary<string, string> this[string @namespace] => _collection[@namespace];

    public IEnumerable<string> Keys => _collection.Keys;
    public IEnumerable<IReadOnlyDictionary<string, string>> Values => _collection.Values;
}
