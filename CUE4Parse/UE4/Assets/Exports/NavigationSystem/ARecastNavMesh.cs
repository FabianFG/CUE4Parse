using System.ComponentModel;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem;

public class ARecastNavMesh : ANavigationData
{
    public float AgentHeight;
    public float AgentRadius;
    public FNavMeshResolutionParam[] NavMeshResolutionParams; 
    
    public ENavMeshVersion NavMeshVersion;
    public FPImplRecastNavMesh? RecastNavMeshImpl;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AgentHeight = GetOrDefault<float>(nameof(AgentHeight));
        AgentRadius = GetOrDefault<float>(nameof(AgentRadius));

        if (!TryGetAllValues(out NavMeshResolutionParams, nameof(NavMeshResolutionParams)))
            NavMeshResolutionParams = new FNavMeshResolutionParam[3];
        
        NavMeshVersion = Ar.Read<ENavMeshVersion>();

        var recastNavMeshSizePos = Ar.Position;
        var recastNavMeshSizeBytes = Ar.Read<uint>();

        if (NavMeshVersion < ENavMeshVersion.NAVMESHVER_MIN_COMPATIBLE)
        {
            Log.Error("NavMeshVersion is too old and not supported: '{0}'", NavMeshVersion);
            Ar.Position = recastNavMeshSizePos + recastNavMeshSizeBytes;
        }
        else if (NavMeshVersion > ENavMeshVersion.Latest)
        {
            Log.Error("NavMeshVersion is too new and not supported: '{0}'", NavMeshVersion);
            Ar.Position = recastNavMeshSizePos + recastNavMeshSizeBytes;
        }
        else if (recastNavMeshSizeBytes > 4)
        {
            RecastNavMeshImpl = new FPImplRecastNavMesh(Ar, this);
        }
    }
    
    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName(nameof(NavMeshVersion));
        serializer.Serialize(writer, NavMeshVersion);
        
        writer.WritePropertyName(nameof(RecastNavMeshImpl));
        serializer.Serialize(writer, RecastNavMeshImpl);
    }
    
    public float GetCellSize(ENavigationDataResolution resolution) => NavMeshResolutionParams[(byte)resolution].CellSize;
    public float GetAgentMaxStepHeight(ENavigationDataResolution resolution) => NavMeshResolutionParams[(byte)resolution].AgentMaxStepHeight;
}

[JsonConverter(typeof(EnumConverter<ENavMeshVersion>))]
public enum ENavMeshVersion
{
    NAVMESHVER_INITIAL                               = 1,
    NAVMESHVER_TILED_GENERATION                      = 2,
    NAVMESHVER_SEAMLESS_REBUILDING_1		         = 3,
    NAVMESHVER_AREA_CLASSES                          = 4,
    NAVMESHVER_CLUSTER_PATH                          = 5,
    NAVMESHVER_SEGMENT_LINKS                         = 6,
    NAVMESHVER_DYNAMIC_LINKS                         = 7,
    NAVMESHVER_64BIT                                 = 9,
    NAVMESHVER_CLUSTER_SIMPLIFIED                    = 10, // UE4.4
    NAVMESHVER_OFFMESH_HEIGHT_BUG                    = 11, // UE4.8
    NAVMESHVER_MIN_COMPATIBLE                        = NAVMESHVER_OFFMESH_HEIGHT_BUG,
    NAVMESHVER_LANDSCAPE_HEIGHT                      = 13,
    NAVMESHVER_LWCOORDS                              = 14,
    NAVMESHVER_OODLE_COMPRESSION                     = 15,
    NAVMESHVER_LWCOORDS_SEREALIZATION                = 17, // Allows for nav meshes to be serialized agnostic of LWCoords being float or double.
    NAVMESHVER_MAXTILES_COUNT_CHANGE                 = 19,
    NAVMESHVER_LWCOORDS_OPTIMIZATION                 = 20,
    NAVMESHVER_OPTIM_FIX_SERIALIZE_PARAMS            = 21, // UE5.0 Fix, serialize params that used to be in the tile and are now in the navmesh.
    NAVMESHVER_MAXTILES_COUNT_SKIP_INCLUSION         = 22,
    NAVMESHVER_TILE_RESOLUTIONS                      = 23, // Addition of a tile resolution index to the tile header.
    NAVMESHVER_TILE_RESOLUTIONS_CELLHEIGHT           = 24, // UE5.2 Addition of CellHeight in the resolution params, deprecating the original CellHeight.
    NAVMESHVER_1_VOXEL_AGENT_STEEP_SLOPE_FILTER_FIX  = 25, // Fix, remove steep slope filtering during heightfield ledge filtering when the agent radius is included into a single voxel
    NAVMESHVER_TILE_RESOLUTIONS_AGENTMAXSTEPHEIGHT   = 26, // UE5.3 Addition of AgentMaxStepHeight in the resolution params, deprecating the original AgentMaxStepHeight.
    NAVMESHVER_NAVLINK_JUMP_CONFIGS                  = 27, // UE5.7 Addition of NavLinkJumpConfigs, deprecating the original NavLinkJumpDownConfig
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1
}

public enum ENavigationDataResolution : byte
{
    Low = 0,
    Default = 1,
    High = 2,
    
    [Description("None")]
    Invalid = 3,
    MAX = 3,
}
