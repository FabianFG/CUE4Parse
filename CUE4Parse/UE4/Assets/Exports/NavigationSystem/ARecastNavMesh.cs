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
    public FPImplRecastNavMesh RecastNavMeshImpl;
    
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

        if (NavMeshVersion < ENavMeshVersion.MinCompatible)
        {
            Log.Error("NavMeshVersion is too old and not supported: '{0}'", NavMeshVersion);
            CleanUpBadVersion();
        }
        else if (NavMeshVersion > ENavMeshVersion.Latest)
        {
            Log.Error("NavMeshVersion is too new and not supported: '{0}'", NavMeshVersion);
            CleanUpBadVersion();
        }
        else if (recastNavMeshSizeBytes > 4)
        {
            RecastNavMeshImpl = new FPImplRecastNavMesh(Ar, this);
        }
        else
        {
            Ar.Position = recastNavMeshSizePos + recastNavMeshSizeBytes;
        }

        return;

        void CleanUpBadVersion()
        {
            Ar.Position = recastNavMeshSizePos + recastNavMeshSizeBytes;
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
    OffMeshHeightBug = 11,
    
    MinCompatible = 20,
    LwCoordsOptimization = MinCompatible,
    
    OptimFixSerializeParams, // Fix, serialize params that used to be in the tile and are now in the navmesh
    MaxTilesCountSkipInclusion,
    TileResolutions, // Addition of a tile resolution index to the tile header.
    TileResolutionsCellHeight, // Addition of CellHeight in the resolution params, deprecating the original CellHeight.
    VoxelAgentSleepSlopeFilterFix, // Fix, remove steep slope filtering during heightfield ledge filtering when the agent radius is included into a single voxel
    TileResolutionsAgentMaxStepHeight, // // Addition of AgentMaxStepHeight in the resolution params, deprecating the original AgentMaxStepHeight.
    NavLinkJumpConfigs, // Addition of NavLinkJumpConfigs, deprecating the original NavLinkJumpDownConfig
    
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