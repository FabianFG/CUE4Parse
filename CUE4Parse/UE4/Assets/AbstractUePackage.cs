using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets;

[JsonConverter(typeof(PackageConverter))]
public abstract class AbstractUePackage : UObject, IPackage
{
    public IFileProvider? Provider { get; }
    public TypeMappings? Mappings => Provider?.MappingsForGame;

    public abstract FPackageFileSummary Summary { get; }
    public abstract FNameEntrySerialized[] NameMap { get; }
    public abstract int ImportMapLength { get; }
    public abstract int ExportMapLength { get; }

    public Lazy<UObject>[] ExportsLazy { get; protected init; }
    public bool IsFullyLoaded { get; protected init; }

    public bool CanDeserialize
    {
        get
        {
            if (HasFlags(EPackageFlags.PKG_UnversionedProperties) && Mappings is null)
                throw new ParserException("Package has unversioned properties but mapping file is missing, can't serialize");
            return true;
        }
    }

    protected AbstractUePackage(string name, IFileProvider? provider)
    {
        Name = name;
        Provider = provider;
        Flags |= EObjectFlags.RF_WasLoaded;
    }

    public UObject ConstructObject(UStruct? struc, IPackage? owner = null, EObjectFlags flags = EObjectFlags.RF_NoFlags)
    {
        UObject? obj = null;
        var mappings = owner?.Mappings;
        var current = struc;
        while (current != null) // Traverse up until a known one is found
        {
            if (current is UClass scriptClass)
            {
                // We know this is a class defined in code at this point
                obj = scriptClass.ConstructObject(flags);
                if (obj != null)
                    break;
            }

            var previous = current;
            current = current.SuperStruct?.Load<UStruct>();

            if (current is null && mappings is not null && mappings.Types.TryGetValue(previous.Name, out var structMappings))
            {
                // added guard for infinite loop
                if (string.IsNullOrEmpty(structMappings.SuperType) || previous.Name == structMappings.SuperType) break;
                current = new UScriptClass(structMappings.SuperType) ;
            }
        }

        obj ??= new UObject();
        obj.Class = struc;
        obj.Flags |= EObjectFlags.RF_WasLoaded;
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DeserializeObject(UObject obj, FAssetArchive Ar, long serialSize)
    {
        var serialOffset = Ar.Position;
        var validPos = serialOffset + serialSize;
        try
        {
            obj.Deserialize(Ar, validPos);
#if DEBUG
            var remaining = validPos - Ar.Position;
            switch (remaining)
            {
                case > 0:
                    Log.Warning("Did not read {0} correctly, {1} bytes remaining ({2}%)", obj.ExportType, remaining,
                        Math.Round((decimal)remaining / validPos * 100, 2));
                    break;
                case < 0:
                    Log.Warning("Did not read {0} correctly, {1} bytes exceeded", obj.ExportType, Math.Abs(remaining));
                    break;
                default:
                    Log.Debug("Successfully read {0} at {1} with size {2}", obj.ExportType, serialOffset, serialSize);
                    break;
            }
#endif
        }
        catch (Exception e)
        {
            if (Globals.FatalObjectSerializationErrors)
            {
                throw new ParserException($"Could not read {obj.ExportType} correctly", e);
            }
            Log.Error(e, "Could not read {0} correctly", obj.ExportType);
        }
    }

    public override bool IsNameStableForNetworking() => true;   // For now, assume all packages have stable net names

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFlags(EPackageFlags flags) => Summary.PackageFlags.HasFlag(flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal);

    public abstract ResolvedObject? ResolvePackageIndex(FPackageIndex? index);

    public override string ToString() => Name;
}

[JsonConverter(typeof(ResolvedObjectConverter))]
public abstract class ResolvedObject : IObject
{
    protected readonly IPackage Package;

    public ResolvedObject(IPackage package, int exportIndex = -1)
    {
        Package = package;
        ExportIndex = exportIndex;
    }

    public int ExportIndex { get; }
    public abstract FName Name { get; }
    public virtual ResolvedObject? Outer => null;
    public virtual ResolvedObject? Class => null;
    public virtual ResolvedObject? Super => null;
    public virtual Lazy<UObject>? Object => ExportIndex >= 0 && ExportIndex < Package.ExportsLazy.Length
        ? Package.ExportsLazy[ExportIndex]
        : null;

    public string GetFullName(bool includeOuterMostName = true, bool includeClassPackage = false)
    {
        var result = new StringBuilder(128);
        GetFullName(includeOuterMostName, result, includeClassPackage);
        return result.ToString();
    }

    public void GetFullName(bool includeOuterMostName, StringBuilder resultString, bool includeClassPackage = false)
    {
        resultString.Append(includeClassPackage ? Class?.GetPathName() : Class?.Name);
        resultString.Append('\'');
        GetPathName(includeOuterMostName, resultString);
        resultString.Append('\'');
    }

    public string GetPathName(bool includeOuterMostName = true)
    {
        var result = new StringBuilder();
        GetPathName(includeOuterMostName, result);
        return result.ToString();
    }

    public void GetPathName(bool includeOuterMostName, StringBuilder resultString)
    {
        var objOuter = Outer;
        if (objOuter != null)
        {
            var objOuterOuter = objOuter.Outer;
            if (objOuterOuter != null || includeOuterMostName)
            {
                objOuter.GetPathName(includeOuterMostName, resultString);
                resultString.Append(objOuterOuter is { Outer: null } ? ':' : '.');
            }
        }

        resultString.Append(Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? Load<T>() where T : UObject => Object?.Value as T;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UObject? Load() => Object?.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryLoad(out UObject export)
    {
        try
        {
            export = Object?.Value;
            return export != null;
        }
        catch
        {
            export = default;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<UObject?> LoadAsync() => await Task.FromResult(Object?.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<UObject?> TryLoadAsync()
    {
        try
        {
            return await Task.FromResult(Object?.Value);
        }
        catch
        {
            return await Task.FromResult<UObject?>(null);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ResolvedObject? other)
        => other != null && ExportIndex == other.ExportIndex && Name == other.Name && Package == other.Package;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is ResolvedObject other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return HashCode.Combine(ExportIndex, Name.GetHashCode(), Package.GetHashCode());
    }

    public override string ToString() => GetFullName();
}

public class ResolvedLoadedObject : ResolvedObject
{
    private readonly UObject _object;

    public ResolvedLoadedObject(UObject obj) : base(obj.Owner)
    {
        _object = obj;
    }

    public override FName Name => new(_object.Name);
    public override ResolvedObject? Outer
    {
        get
        {
            var obj = _object.Outer;
            return obj != null ? new ResolvedLoadedObject(obj) : null;
        }
    }
    public override ResolvedObject? Class
    {
        get
        {
            var obj = _object.Class;
            return obj != null ? new ResolvedLoadedObject(obj) : null;
        }
    }
    public override ResolvedObject? Super => null; //new ResolvedLoadedObject(_object.Super);
    public override Lazy<UObject> Object => new(() => _object);
}
