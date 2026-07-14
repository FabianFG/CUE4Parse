using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

[JsonConverter(typeof(FStaticMeshSectionConverter))]
public class FStaticMeshSection
{
    public int MaterialIndex;
    public int FirstIndex;
    public int NumTriangles;
    public int MinVertexIndex;
    public int MaxVertexIndex;
    public bool bEnableCollision;
    public bool bCastShadow;
    public bool bForceOpaque;
    public bool bVisibleInRayTracing;
    public bool bAffectDistanceFieldLighting;
    public int? CustomData;

    public FStaticMeshSection() { }

    public FStaticMeshSection(FArchive Ar)
    {
        MaterialIndex = Ar.Read<int>();
        FirstIndex = Ar.Read<int>();
        NumTriangles = Ar.Read<int>();
        MinVertexIndex = Ar.Read<int>();
        MaxVertexIndex = Ar.Read<int>();
        bEnableCollision = Ar.ReadBoolean();
        bCastShadow = Ar.ReadBoolean();
        if (Ar.Game == GAME_PlayerUnknownsBattlegrounds) Ar.Position += 5; // byte + int
        if (Ar.Game == GAME_NeedForSpeedMobile) CustomData = Ar.Read<int>();
        if (Ar.Game is GAME_AssaultFireFuture) return;
        if (Ar.Game is GAME_ArenaBreakoutMobile) Ar.Position += 4;
        bForceOpaque = FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StaticMeshSectionForceOpaqueField && Ar.ReadBoolean();
        if (Ar.Game is GAME_MortalKombat1 or GAME_TheFinals or GAME_ArcRaiders) Ar.Position += 8;
        if (Ar.Game == GAME_BlueProtocol) CustomData = Ar.Read<short>(); // Must be read before bVisibleInRayTracing
        bVisibleInRayTracing = !Ar.Versions["StaticMesh.HasVisibleInRayTracing"] || Ar.ReadBoolean();
        if (Ar.Game is GAME_Grounded or GAME_Dauntless) Ar.Position += 8;
        if (Ar.Game is GAME_ValorantSource) Ar.Position += 12;
        bAffectDistanceFieldLighting = Ar.Game >= GAME_UE5_1 && Ar.ReadBoolean();
        if (Ar.Game is GAME_RogueCompany or GAME_Grounded or GAME_Grounded2 or GAME_RacingMaster or GAME_WutheringWaves
            or GAME_MetroAwakening or GAME_Avowed or GAME_OutlastTrials or GAME_OuterWorlds2 or GAME_LiesofP) Ar.Position += 4;
        if (Ar.Game is GAME_InfinityNikki)
        {
            CustomData = Ar.Read<int>();
            Ar.Position += 8;
        }
    }
}
