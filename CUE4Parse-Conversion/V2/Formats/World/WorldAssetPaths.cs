using System.Collections.Generic;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Formats.World;

public sealed class WorldAssetPaths
{
    public Dictionary<FPackageIndex, string> Meshes { get; } = [];
    public Dictionary<FPackageIndex, string> Materials { get; } = [];
    public Dictionary<string, string> Worlds { get; } = [];
    public List<string> SubLayers { get; } = [];
}
