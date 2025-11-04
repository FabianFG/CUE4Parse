using CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem;

public class FPImplRecastNavMesh
{
    public DetourNavMeshParams DetourNavMeshParams;
    public DetourMeshTile[] DetourMeshTiles;

    private const int DT_RESOLUTION_COUNT = 3;
    
    public FPImplRecastNavMesh(FArchive Ar, ARecastNavMesh navMeshOwner)
    {
        var numTiles = Ar.Read<int>();

        DetourNavMeshParams = new DetourNavMeshParams
        {
            Orig = Ar.ReadArray(3, Ar.ReadFReal),
            TileWidth = Ar.ReadFReal(),
            TileHeight = Ar.ReadFReal(),
            MaxTiles = Ar.Read<int>(),
            MaxPolys = Ar.Read<int>()
        };

        if (navMeshOwner.NavMeshVersion >= ENavMeshVersion.TileResolutions)
        {
            DetourNavMeshParams.WalkableHeight   =  Ar.ReadFReal();
            DetourNavMeshParams.WalkableRadius   =  Ar.ReadFReal();
            DetourNavMeshParams.WalkableClimb    =  Ar.ReadFReal();
            DetourNavMeshParams.ResolutionParams = Ar.ReadArray(DT_RESOLUTION_COUNT, () => new DetourNavMeshResParams(Ar.ReadFReal()));
        }
        else if (navMeshOwner.NavMeshVersion >= ENavMeshVersion.OptimFixSerializeParams)
        {
            DetourNavMeshParams.WalkableHeight =  Ar.ReadFReal();
            DetourNavMeshParams.WalkableRadius =  Ar.ReadFReal();
            DetourNavMeshParams.WalkableClimb  =  Ar.ReadFReal();
            
            var defaultQuantFactor = new DetourNavMeshResParams(Ar.ReadFReal());
            DetourNavMeshParams.ResolutionParams =
            [
                defaultQuantFactor,
                defaultQuantFactor,
                defaultQuantFactor
            ];
        }
        else
        {
            DetourNavMeshParams.WalkableHeight = navMeshOwner.AgentHeight;
            DetourNavMeshParams.WalkableRadius = navMeshOwner.AgentRadius;
            DetourNavMeshParams.WalkableClimb = navMeshOwner.GetAgentMaxStepHeight(ENavigationDataResolution.Default);

            var defaultQuantFactor = new DetourNavMeshResParams(1f / navMeshOwner.GetCellSize(ENavigationDataResolution.Default));
            DetourNavMeshParams.ResolutionParams =
            [
                defaultQuantFactor,
                defaultQuantFactor,
                defaultQuantFactor
            ];
        }
        
        DetourMeshTiles = new DetourMeshTile[numTiles];
        for (int i = 0; i < numTiles; i++)
        {
            var tileRef = Ar.Read<ulong>();
            var tileDataSize = Ar.Read<int>();
                
            if (tileRef == ulong.MaxValue || tileDataSize == 0)
                continue;
                
            SerializeRecastMeshTile(Ar, navMeshOwner.NavMeshVersion, i);
            SerializeCompressedTileCacheData(Ar);
        }
    }

    private void SerializeRecastMeshTile(FArchive Ar, ENavMeshVersion navMeshVersion, int index)
    {
        var sizeInfo = new FDetourTileSizeInfo(Ar);
        DetourMeshTiles[index] = new DetourMeshTile(Ar, sizeInfo, navMeshVersion);
    }

    private void SerializeCompressedTileCacheData(FArchive Ar)
    {
        var compressedDataSizeNoHeader = Ar.Read<int>();
        var bHasHeader = compressedDataSizeNoHeader >= 0;

        if (!bHasHeader) return;

        var header = new DetourTileCacheLayerHeader(Ar);

        if (compressedDataSizeNoHeader > 0)
            Ar.Position += compressedDataSizeNoHeader;
    }
}