using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem;

[StructFallback]
public struct FNavMeshResolutionParam
{
    public float CellSize;
    public float CellHeight;
    public float AgentMaxStepHeight;
    
    public FNavMeshResolutionParam(FStructFallback fallback)
    {
        CellSize = fallback.GetOrDefault(nameof(CellSize), 25f);
        CellHeight = fallback.GetOrDefault(nameof(CellHeight), 10f);
        AgentMaxStepHeight = fallback.GetOrDefault(nameof(AgentMaxStepHeight), 35f);
    }
}