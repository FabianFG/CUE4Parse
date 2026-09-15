using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

[JsonConverter(typeof(FStaticMeshSectionConverter))]
public class FStaticMeshSection
{
    public FPackageIndex? Material;
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
        var sectionStart = Ar.Position;
        bool ReadSectionBoolean(string field)
        {
            if ((Ar.Game is GAME_Splitgate2 or GAME_Empulse) && Ar.Length - Ar.Position >= 4)
            {
                var raw = BitConverter.ToInt32(Ar.ReadBytesAt(Ar.Position, 4), 0);
                if (raw is not (0 or 1))
                {
                    var sampleStart = Math.Max(0, sectionStart - 8);
                    var sample = Ar.ReadBytesAt(sampleStart, (int)Math.Min(96, Ar.Length - sampleStart));
                    Serilog.Log.Warning("[StaticMeshDiag] {Archive}: section start={Start}; invalid {Field} at {Position}={Raw}; material={Material}; first index={First}; triangles={Triangles}; vertex range={Min}-{Max}; bytes at {SampleStart}={Sample}",
                        Ar.Name, sectionStart, field, Ar.Position, raw, MaterialIndex, FirstIndex, NumTriangles, MinVertexIndex, MaxVertexIndex, sampleStart, Convert.ToHexString(sample));
                }
            }
            return Ar.ReadBoolean();
        }

        if (Ar.Game < GAME_UE4_0)
        {
            Material = new FPackageIndex((FAssetArchive)Ar); // Material
            bEnableCollision = ReadSectionBoolean("bEnableCollision");
            Ar.ReadBoolean(); // OldEnableCollision
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.AddedCastShadow) bCastShadow = ReadSectionBoolean("bCastShadow");
        }
        else
        {
            MaterialIndex = Ar.Read<int>();
        }
        FirstIndex = Ar.Read<int>();
        NumTriangles = Ar.Read<int>();
        MinVertexIndex = Ar.Read<int>();
        MaxVertexIndex = Ar.Read<int>();
        if (Ar.Game >= GAME_UE4_0)
        {
            bEnableCollision = ReadSectionBoolean("bEnableCollision");
            bCastShadow = ReadSectionBoolean("bCastShadow");
        }
        else
        {
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_VERSION_16) MaterialIndex = Ar.Read<int>();
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_FRAGMENTINDEX) Ar.SkipFixedArray(8); // Fragment
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_PLATFORMMESHDATA)
            {
                var bLoadPlatformData = Ar.ReadFlag();
                if (bLoadPlatformData)
                {
                    new FPS3StaticMeshData(Ar);
                }
            }
        }
        if (Ar.Game == GAME_PlayerUnknownsBattlegrounds) Ar.Position += 5; // byte + int
        if (Ar.Game == GAME_NeedForSpeedMobile) CustomData = Ar.Read<int>();
        if (Ar.Game is GAME_AssaultFireFuture) return;
        if (Ar.Game is GAME_ArenaBreakoutMobile) Ar.Position += 4;
        bForceOpaque = FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StaticMeshSectionForceOpaqueField && ReadSectionBoolean("bForceOpaque");
        if (Ar.Game is GAME_MortalKombat1 or GAME_TheFinals or GAME_ArcRaiders) Ar.Position += 8;
        if (Ar.Game == GAME_BlueProtocol) CustomData = Ar.Read<short>(); // Must be read before bVisibleInRayTracing
        if (Ar.Game is GAME_WutheringWaves) Ar.SkipFixedArray(sizeof(int));
        bVisibleInRayTracing = !Ar.Versions["StaticMesh.HasVisibleInRayTracing"] || ReadSectionBoolean("bVisibleInRayTracing");
        if (Ar.Game is GAME_Grounded or GAME_Dauntless) Ar.Position += 8;
        if (Ar.Game is GAME_ValorantSource) Ar.Position += 12;
        bAffectDistanceFieldLighting = Ar.Game >= GAME_UE5_1 && ReadSectionBoolean("bAffectDistanceFieldLighting");
        if (Ar.Game is GAME_RogueCompany or GAME_Grounded or GAME_Grounded2 or GAME_RacingMaster
            or GAME_MetroAwakening or GAME_Avowed or GAME_OutlastTrials or GAME_OuterWorlds2 or GAME_LiesofP) Ar.Position += 4;
        if (Ar.Game is GAME_InfinityNikki)
        {
            CustomData = Ar.Read<int>();
            Ar.Position += 8;
        }
    }
}
