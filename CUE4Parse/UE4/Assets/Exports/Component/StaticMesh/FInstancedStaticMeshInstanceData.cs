using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;

public class FInstancedStaticMeshInstanceData
{
    private readonly FMatrix Transform; // don't expose the raw matrix for now

    public readonly FTransform TransformData;

    public FInstancedStaticMeshInstanceData(FArchive Ar)
    {
        Transform = new FMatrix(Ar);

        Ar.Position += Ar.Game switch
        {
            GAME_HogwartsLegacy => Ar.Read<int>() * sizeof(int) + 4,
            GAME_AWayOut or GAME_PlayerUnknownsBattlegrounds or GAME_SeaOfThieves or GAME_AceCombat7
                or GAME_DaysGone or GAME_InfinityNikki or GAME_NarutotoBorutoShinobiStriker or GAME_eFootball
                or GAME_DragonQuestXI or GAME_WeHappyFew or GAME_CodeVein or GAME_TheDivisionResurgence or < GAME_UE4_0 => 16, // sizeof(FVector2D) * 2; LightmapUVBias, ShadowmapUVBias
            GAME_SilentHill2Remake or GAME_StateOfDecay2 or GAME_ThePathless or GAME_Abzu => 32,// probably LightmapUVBias, ShadowmapUVBias as FVector2d * 2
            _ => 0,
        };
        TransformData.SetFromMatrix(Transform);
    }

    public FInstancedStaticMeshInstanceData(FMatrix matrix)
    {
        Transform = matrix;
        TransformData.SetFromMatrix(Transform);
    }

    public FInstancedStaticMeshInstanceData(FTransform transform)
    {
        Transform = null!;
        TransformData = transform;
    }

    public override string ToString()
    {
        return TransformData.ToString();
    }
}
