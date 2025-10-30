namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

/// <summary>
/// Configuration parameters used to define multi-tile navigation meshes.
/// The values are used to allocate space during the initialization of a navigation mesh.
/// </summary>
public struct DetourNavMeshParams
{
    /// <summary>
    /// The world space origin of the navigation mesh's tile space. [(x, y, z)]
    /// </summary>
    public float[] Orig;

    /// <summary>
    /// The width of each tile. (Along the x-axis.)
    /// </summary>
    public float TileWidth;
    
    /// <summary>
    /// The height of each tile. (Along the z-axis.)
    /// </summary>
    public float TileHeight;

    /// <summary>
    /// The maximum number of tiles the navigation mesh can contain.
    /// </summary>
    public int MaxTiles;

    /// <summary>
    /// The maximum number of polygons each tile can contain.
    /// </summary>
    public int MaxPolys;

    /// <summary>
    /// The height of the agents using the tile.
    /// </summary>
    public float WalkableHeight;
    
    /// <summary>
    /// The radius of the agents using the tile.
    /// </summary>
    public float WalkableRadius;
    
    /// <summary>
    /// The maximum climb height of the agents using the tile.	
    /// </summary>
    public float WalkableClimb;

    /// <summary>
    /// Parameters depending on resolutions.
    /// </summary>
    public DetourNavMeshResParams[] ResolutionParams;
}