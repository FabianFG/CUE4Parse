using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;

public class FInstancedStaticMeshInstanceData
{
    private readonly FMatrix Transform; // don't expose the raw matrix for now

    public readonly FTransform TransformData = new();

    public FInstancedStaticMeshInstanceData(FArchive Ar)
    {
        Transform = new FMatrix(Ar);

        if (Ar.Game == EGame.GAME_HogwartsLegacy)
            Ar.SkipFixedArray(sizeof(int));
        if (Ar.Game is EGame.GAME_AWayOut or EGame.GAME_PlayerUnknownsBattlegrounds or EGame.GAME_SeaOfThieves)
            Ar.Position += 16; // sizeof(FVector2D) * 2; LightmapUVBias, ShadowmapUVBias
        if (Ar.Game == EGame.GAME_SilentHill2Remake)
            Ar.Position += 32; // probably LightmapUVBias, ShadowmapUVBias as FVector2d * 2
        TransformData.SetFromMatrix(Transform);
    }

    public override string ToString()
    {
        return TransformData.ToString();
    }
}
