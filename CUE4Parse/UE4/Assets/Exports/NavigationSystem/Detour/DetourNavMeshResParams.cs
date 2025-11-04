namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourNavMeshResParams
{
    /// <summary>
    /// The bounding volume quantization factor.
    /// </summary>
    public float BvQuantFactor;

    public DetourNavMeshResParams(float bvQuantFactor)
    {
        BvQuantFactor = bvQuantFactor;
    }
}