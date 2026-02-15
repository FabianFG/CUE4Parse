using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;
using CUE4Parse.UE4.Objects.NavigationSystem.NavMesh;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem;

public class FPImplRecastNavMesh
{
    public DetourNavMeshParams DetourNavMeshParams;
    public FRecastTileData[] DetourMeshTiles;

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

        if (navMeshOwner.NavMeshVersion >= ENavMeshVersion.NAVMESHVER_TILE_RESOLUTIONS)
        {
            DetourNavMeshParams.WalkableHeight   =  Ar.ReadFReal();
            DetourNavMeshParams.WalkableRadius   =  Ar.ReadFReal();
            DetourNavMeshParams.WalkableClimb    =  Ar.ReadFReal();
            DetourNavMeshParams.ResolutionParams = Ar.ReadArray(DT_RESOLUTION_COUNT, () => new DetourNavMeshResParams(Ar.ReadFReal()));
        }
        else if (navMeshOwner.NavMeshVersion >= ENavMeshVersion.NAVMESHVER_OPTIM_FIX_SERIALIZE_PARAMS)
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
        
        DetourMeshTiles = new FRecastTileData[numTiles];
        for (int i = 0; i < numTiles; i++)
        {
            var tileRef = Ar.Read<ulong>();
            if (tileRef == ulong.MaxValue) continue;
            DetourMeshTiles[i] = new FRecastTileData(Ar, navMeshOwner.NavMeshVersion);
        }
    }

    public static FCompressedTileCacheData? SerializeCompressedTileCacheData(FArchive Ar)
    {
        var compressedDataSizeNoHeader = Ar.Read<int>();
        var bHasHeader = compressedDataSizeNoHeader > 0;

        if (!bHasHeader) return null;

        return new FCompressedTileCacheData(Ar, compressedDataSizeNoHeader);
    }
}

public class FCompressedTileCacheData
{
    public DetourTileCacheLayerHeader? Header;
    public int UncompressedSize;
    public int CompressedSize;
    [JsonIgnore] public byte[] CompressedData = [];

    public bool IsValid => Header.HasValue;

    public FCompressedTileCacheData(FArchive Ar, int compressedDataSizeNoHeader)
    {
        var bHasHeader = compressedDataSizeNoHeader >= 0;
        if (!bHasHeader) return;

        Header = new DetourTileCacheLayerHeader(Ar);
        if (Ar.Game <  Versions.EGame.GAME_UE5_0)
            compressedDataSizeNoHeader -= Header.Value.Size(Ar); 

        if (compressedDataSizeNoHeader > 4)
        {
            UncompressedSize = Ar.Read<int>();
            CompressedSize = compressedDataSizeNoHeader - 4;
            CompressedData = Ar.ReadArray<byte>(CompressedSize);
        }
    }

    public byte[] DecompressData()
    {
        if (CompressedData.Length == 0 || UncompressedSize == 0 || CompressedSize == 0)
        {
            return [];
        }
        var decompressedData = new byte[UncompressedSize];
        Compression.Compression.Decompress(CompressedData, 0, CompressedSize, decompressedData, 0, UncompressedSize, CompressionMethod.Oodle);
        return decompressedData;
    }
}
