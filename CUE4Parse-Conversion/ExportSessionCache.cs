using System.Collections.Concurrent;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Materials;

namespace CUE4Parse_Conversion;

/// <summary>Reuses parsed materials and saved texture paths during one export run.</summary>
public sealed class ExportSessionCache
{
    private readonly ConcurrentDictionary<object, object> _entries = new();

    private static object GetAssetKey(UObject source)
        => source.Owner == null ? source : source.GetPathName().ToUpperInvariant();

    public PbrMaterialDto ResolveMaterial(UMaterialInterface material, EMaterialDepth depth)
        => GetMaterialParameters(material, depth).Pbr;

    public CMaterialParams2 ReadParameters(UMaterialInterface material, EMaterialDepth depth)
        => GetMaterialParameters(material, depth).Parameters;

    private MaterialParameters GetMaterialParameters(UMaterialInterface material, EMaterialDepth depth)
    {
        return ((Lazy<MaterialParameters>) _entries.GetOrAdd((GetAssetKey(material), depth), static (_, state) =>
            new Lazy<MaterialParameters>(() => new MaterialParameters(state.Material, state.Depth, state.Cache.GetMaterialParameters)),
            (Material: material, Depth: depth, Cache: this))).Value;
    }

    // Called only after the first mip's exported file has been written successfully
    internal void CacheTexturePath(UTexture texture, string path)
        => _entries[GetAssetKey(texture)] = path;
    
    /// <summary>Returns the saved texture path, if export succeeded.</summary>
    public string? GetTexturePath(UTexture texture)
    {
        if (_entries.TryGetValue(GetAssetKey(texture), out var entry) && entry is string path && File.Exists(path))
            return path;

        return null;
    }

    internal void Clear() => _entries.Clear();
}
