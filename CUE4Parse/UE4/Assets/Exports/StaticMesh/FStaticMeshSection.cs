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
    public int CustomData;

    public FStaticMeshSection(FArchive Ar)
    {
        MaterialIndex = Ar.Read<int>();
        FirstIndex = Ar.Read<int>();
        NumTriangles = Ar.Read<int>();
        MinVertexIndex = Ar.Read<int>();
        MaxVertexIndex = Ar.Read<int>();
        bEnableCollision = Ar.ReadBoolean();
        bCastShadow = Ar.ReadBoolean();
        if (Ar.Game == EGame.GAME_PlayerUnknownsBattlegrounds) Ar.Position += 5; // byte + int
        bForceOpaque = FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StaticMeshSectionForceOpaqueField && Ar.ReadBoolean();
        if (Ar.Game == EGame.GAME_MortalKombat1) Ar.Position += 8; // "None" FName
        bVisibleInRayTracing = !Ar.Versions["StaticMesh.HasVisibleInRayTracing"] || Ar.ReadBoolean();
        if (Ar.Game is EGame.GAME_Grounded or EGame.GAME_Dauntless) Ar.Position += 8;
        bAffectDistanceFieldLighting = Ar.Game >= EGame.GAME_UE5_1 && Ar.ReadBoolean();
        if (Ar.Game is EGame.GAME_RogueCompany or EGame.GAME_Grounded or EGame.GAME_Grounded2 or EGame.GAME_RacingMaster
            or EGame.GAME_MetroAwakening or EGame.GAME_Avowed) Ar.Position += 4;
        if (Ar.Game is EGame.GAME_InfinityNikki)
        {
            CustomData = Ar.Read<int>();
            Ar.Position += 8;
        }
    }
}
