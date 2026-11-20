using System.Collections.Generic;
using CUE4Parse_Conversion.Dto;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Formats.World;

public sealed class WorldAssetPaths
{
    public Dictionary<FPackageIndex, string> Assets { get; } = []; // meshes and materials
    public Dictionary<string, string> Worlds { get; } = [];
    public List<string> SubLayers { get; } = [];

    public Dictionary<LandscapeMeshComponentDto, string> LandscapeMeshes { get; } = [];
    public Dictionary<SplineMeshComponentDto, string> SplineMeshes { get; } = [];

    public bool TryGet(FPackageIndex index, out string path)
    {
        if (!index.IsNull && Assets.TryGetValue(index, out var p) && !string.IsNullOrEmpty(p))
        {
            path = p;
            return true;
        }
        path = string.Empty;
        return false;
    }
}
