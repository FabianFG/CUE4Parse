using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CUE4Parse_Conversion.USD;

public enum UsdPrimSpecifier { Def, Over, Class }
public enum UsdVariability { Uniform, Config, Varying }
public enum UsdListOpType { Explicit, Add, Append, Delete, Order, Prepend }
public enum UsdValueKind { None, Raw, Bool, Int, Long, Float, Double, String, Token, Path, AssetPath, Array, Tuple, Declared }

public readonly record struct UsdMetadata(string Name, UsdValue Value);

public readonly record struct UsdReference(string AssetPath, string? PrimPath = null)
{
    public override string ToString() => PrimPath is { Length: > 0 } ? $"{AssetPath}#{PrimPath}" : AssetPath;
}

public sealed class UsdReferenceList(IEnumerable<UsdReference> references, UsdListOpType operation = UsdListOpType.Prepend)
{
    public List<UsdReference> References { get; } = [.. references];
    public UsdListOpType Operation { get; } = operation;
}

public readonly record struct UsdValue(UsdValueKind Kind, object? RawValue)
{
    public static readonly UsdValue Null = new(UsdValueKind.None, null);
    public static readonly UsdValue Declared = new(UsdValueKind.Declared, null);

    public static UsdValue Raw(string value) => new(UsdValueKind.Raw, value ?? throw new ArgumentNullException(nameof(value)));
    public static UsdValue Bool(bool value) => new(UsdValueKind.Bool, value);
    public static UsdValue Int(int value) => new(UsdValueKind.Int, value);
    public static UsdValue Long(long value) => new(UsdValueKind.Long, value);
    public static UsdValue Float(float value) => new(UsdValueKind.Float, value);
    public static UsdValue Double(double value) => new(UsdValueKind.Double, value);
    public static UsdValue String(string value) => new(UsdValueKind.String, value ?? throw new ArgumentNullException(nameof(value)));
    public static UsdValue Token(string value) => new(UsdValueKind.Token, value ?? throw new ArgumentNullException(nameof(value)));
    public static UsdValue Path(string value) => new(UsdValueKind.Path, value ?? throw new ArgumentNullException(nameof(value)));
    public static UsdValue AssetPath(string value) => new(UsdValueKind.AssetPath, value ?? throw new ArgumentNullException(nameof(value)));

    public static UsdValue Array(IEnumerable<UsdValue> values) => new(UsdValueKind.Array, values.ToArray());
    public static UsdValue Array(params object?[] values) => new(UsdValueKind.Array, values.Select(From).ToArray());
    public static UsdValue Array<T>(IEnumerable<T> values) => new(UsdValueKind.Array, values.Select(v => From((object?) v)).ToArray());
    public static UsdValue Tuple(IEnumerable<UsdValue> values) => new(UsdValueKind.Tuple, values.ToArray());
    public static UsdValue Tuple(params object?[] values) => new(UsdValueKind.Tuple, values.Select(From).ToArray());

    // Implicit conversions – hide boilerplate at call sites
    public static implicit operator UsdValue(bool value) => Bool(value);
    public static implicit operator UsdValue(int value) => Int(value);
    public static implicit operator UsdValue(float value) => Float(value);
    public static implicit operator UsdValue(double value) => Double(value);
    public static implicit operator UsdValue(string value) => String(value);

    public static UsdValue From(object? value)
    {
        return value switch
        {
            null => Null,
            UsdValue usdValue => usdValue,
            bool b => Bool(b),
            byte b => Int(b),
            sbyte b => Int(b),
            short s => Int(s),
            ushort s => Int(s),
            int i => Int(i),
            uint i => Long(i),
            long l => Long(l),
            ulong l and <= long.MaxValue => Long((long) l),
            float f => Float(f),
            double d => Double(d),
            decimal d => Double((double) d),
            string s => String(s),
            IEnumerable<UsdValue> values => Array(values),
            IEnumerable enumerable => Array(Enumerate(enumerable).ToArray()),
            Vector2 v2 => Tuple(v2.X, v2.Y),
            Vector3 v3 => Tuple(v3.X, v3.Y, v3.Z),
            Vector4 v4 => Tuple(v4.X, v4.Y, v4.Z, v4.W),
            Quaternion q => Tuple(q.W, q.X, q.Y, q.Z),
            Matrix4x4 m => Tuple(Tuple(m.M11, m.M12, m.M13, m.M14), Tuple(m.M21, m.M22, m.M23, m.M24), Tuple(m.M31, m.M32, m.M33, m.M34), Tuple(m.M41, m.M42, m.M43, m.M44)),
            ulong => throw new ArgumentOutOfRangeException(nameof(value), value, "USD writer does not support ulong values larger than Int64.MaxValue."),
            _ => throw new NotSupportedException($"Unsupported USD value type '{value.GetType().FullName}'.")
        };
    }

    private static IEnumerable<UsdValue> Enumerate(IEnumerable enumerable)
    {
        foreach (var item in enumerable)
            yield return From(item);
    }

    public IReadOnlyList<UsdValue> AsValues()
    {
        if (RawValue is IReadOnlyList<UsdValue> values) return values;
        if (RawValue is UsdValue[] array) return array;
        throw new InvalidOperationException($"USD value of kind {Kind} does not contain a value list.");
    }

    public string AsString() => RawValue as string ?? throw new InvalidOperationException($"USD value of kind {Kind} does not contain a string.");

    public string FormatScalarInvariant()
    {
        return Kind switch
        {
            UsdValueKind.Bool => ((bool) RawValue!).ToString().ToLowerInvariant(),
            UsdValueKind.Int => ((int) RawValue!).ToString(CultureInfo.InvariantCulture),
            UsdValueKind.Long => ((long) RawValue!).ToString(CultureInfo.InvariantCulture),
            UsdValueKind.Float => ((float) RawValue!).ToString("R", CultureInfo.InvariantCulture),
            UsdValueKind.Double => ((double) RawValue!).ToString("R", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"USD value kind {Kind} is not a scalar number or bool.")
        };
    }
}

public abstract class UsdProperty(string name)
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public bool Custom { get; init; }
    public UsdVariability? Variability { get; init; }
    public List<UsdMetadata> Metadata { get; } = [];

    public T AddMetadata<T>(string name, UsdValue value) where T : UsdProperty
    {
        Metadata.Add(new UsdMetadata(name, value));
        return (T) this;
    }
}

public sealed class UsdAttribute(string typeName, string name, UsdValue value) : UsdProperty(name)
{
    public string TypeName { get; } = typeName ?? throw new ArgumentNullException(nameof(typeName));
    public UsdValue Value { get; } = value;
    public UsdValue[][]? TimeSamples { get; private init; } // TODO: that's kinda ugly to have this here

    // Factory helpers – avoid scattered object-initialiser noise
    public static UsdAttribute Uniform(string typeName, string name, UsdValue value) => new(typeName, name, value) { Variability = UsdVariability.Uniform };
    public static UsdAttribute Flagged(string typeName, string name, UsdValue value) => new(typeName, name, value) { Custom = true };
    public static UsdAttribute CustomUniform(string typeName, string name, UsdValue value) => new(typeName, name, value) { Custom = true, Variability = UsdVariability.Uniform };
    public static UsdAttribute TimeSampled(string typeName, string name, UsdValue[][] samples) => new(typeName, name, UsdValue.Null) { TimeSamples = samples };

    /// <summary>Creates a primvar attribute with the given interpolation.</summary>
    public static UsdAttribute Primvar(string typeName, string name, UsdValue values, string? interpolation = null, params UsdMetadata[] metadata)
    {
        var attr = new UsdAttribute(typeName, name, values) { Custom = false };
        if (interpolation != null) attr.Metadata.Add(new UsdMetadata("interpolation", UsdValue.String(interpolation)));
        attr.Metadata.AddRange(metadata);
        return attr;
    }
}

public sealed class UsdRelationship(string name, params UsdPrim[] targets) : UsdProperty(name)
{
    public string[] GetPaths() => targets.Select(p => p.GetPath()).ToArray();
}

public sealed class UsdPrim(string typeName, string name, UsdPrimSpecifier specifier = UsdPrimSpecifier.Def)
{
    public string TypeName { get; } = typeName ?? throw new ArgumentNullException(nameof(typeName));
    public string Name { get; } = SanitizeIdentifier(name ?? throw new ArgumentNullException(nameof(name)));

    private string? _disambiguatedName;
    public string EffectiveName => _disambiguatedName ?? Name;

    public UsdPrimSpecifier Specifier { get; } = specifier;
    public UsdPrim? Parent { get; private set; }

    public List<UsdMetadata> Metadata { get; } = [];
    public List<UsdProperty> Properties { get; } = [];
    public List<UsdPrim> Children { get; } = [];
    public UsdReferenceList? References { get; private set; }

    private HashSet<string>? _takenChildNames;

    public string GetPath() => $"{Parent?.GetPath()}/{EffectiveName}";

    public UsdPrim Add<T>(params T[] properties) where T : UsdProperty
    {
        Properties.AddRange(properties);
        return this;
    }

    public UsdPrim Add(params UsdPrim[] children)
    {
        _takenChildNames ??= new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in children)
        {
            if (!_takenChildNames.Add(child.Name))
            {
                var counter = 1;
                string candidate;
                do { candidate = $"{child.Name}_{counter++}"; }
                while (!_takenChildNames.Add(candidate));
                child._disambiguatedName = candidate;
            }

            child.Parent = this;
            Children.Add(child);
        }
        return this;
    }

    public UsdPrim AddMetadata(string name, UsdValue value)
    {
        Metadata.Add(new UsdMetadata(name, value));
        return this;
    }

    /// <summary>Adds a primvar attribute with the given interpolation.</summary>
    public UsdPrim AddPrimvar(string typeName, string name, UsdValue values, string? interpolation = null, params UsdMetadata[] metadata)
    {
        Properties.Add(UsdAttribute.Primvar(typeName, name, values, interpolation, metadata));
        return this;
    }

    public UsdPrim SetReference(UsdReferenceList references)
    {
        References = references;
        return this;
    }

    public static UsdPrim Def(string typeName, string name) => new(typeName, name);
    public static UsdPrim Over(string typeName, string name) => new(typeName, name, UsdPrimSpecifier.Over);
    public static UsdPrim Class(string typeName, string name) => new(typeName, name, UsdPrimSpecifier.Class);

    private static string SanitizeIdentifier(string name)
    {
        if (name.Length == 0) return "_unnamed";

        // Fast path: already valid
        var needsLeadingUnderscore = char.IsAsciiDigit(name[0]);
        var anyInvalid = needsLeadingUnderscore;
        if (!anyInvalid)
        {
            foreach (var c in name)
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                {
                    anyInvalid = true;
                    break;
                }
            }
        }
        if (!anyInvalid) return name;

        // Build sanitised string
        var offset = needsLeadingUnderscore ? 1 : 0;
        var buf = new char[name.Length + offset];
        if (needsLeadingUnderscore) buf[0] = '_';

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            buf[i + offset] = char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_';
        }

        var result = new string(buf);
        return result.Length == 0 ? "_unnamed" : result;
    }
}

public sealed class UsdStage
{
    public const string Version = "1.0";

    public List<UsdMetadata> Metadata { get; } = [];
    public List<UsdPrim> Prims { get; } = [];

    public UsdStage(string defaultPrim)
    {
        AddMetadata("defaultPrim", defaultPrim);
        AddMetadata("metersPerUnit", 0.01f);
        AddMetadata("upAxis", "Z");
    }

    public UsdStage(UsdPrim defaultPrim)
    {
        AddMetadata("defaultPrim", defaultPrim.Name);
        AddMetadata("metersPerUnit", 0.01f);
        AddMetadata("upAxis", "Z");
        Add(defaultPrim);
    }

    public UsdStage Add(params UsdPrim[] prims)
    {
        Prims.AddRange(prims);
        return this;
    }

    public UsdStage AddMetadata(string name, string value) => AddMetadata(name, UsdValue.String(value));
    public UsdStage AddMetadata(string name, double value) => AddMetadata(name, UsdValue.Double(value));
    public UsdStage AddMetadata(string name, float value) => AddMetadata(name, UsdValue.Float(value));
    public UsdStage AddMetadata(string name, UsdValue value)
    {
        Metadata.Add(new UsdMetadata(name, value));
        return this;
    }

    public string SerializeToString() => UsdaWriter.Serialize(this);
    public byte[] SerializeToBinary() => Encoding.UTF8.GetBytes(SerializeToString());
}
