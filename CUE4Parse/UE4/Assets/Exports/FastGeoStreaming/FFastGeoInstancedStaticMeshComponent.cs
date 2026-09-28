using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.InstanceData;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

[StructLayout(LayoutKind.Sequential)]
public struct FInstancedStaticMeshRandomSeed
{
    public int StartInstanceIndex;
    public int RandomSeed;
}

public class FFastGeoInstancedStaticMeshComponent : FFastGeoStaticMeshComponentBase
{
    public bool bUseHighPrecisionPerInstanceSMData;
    public FInstancedStaticMeshInstanceData[] PerInstanceSMData;
    public int LastInstanceBodyIndex;
    public int InstancingRandomSeed;
    public float[] PerInstanceSMCustomData;
    public FInstancedStaticMeshRandomSeed[] AdditionalRandomSeeds;
    public FBox NavigationBounds;
    public FCompressedSpatialHashItem[] SpatialHashes;
    public float[] PerInstanceRandomIDs;

    public FFastGeoInstancedStaticMeshComponent(FFastGeoArchive Ar) : base(Ar)
    {
        bUseHighPrecisionPerInstanceSMData = Ar.Game switch {
            >= GAME_UE5_8 => Ar.ReadBoolean(),
            _ => true,
        };

        // i think it's FMatrix3x4, where 3x3 is scale and rotation, and last row is translation
        if (Ar.Game is GAME_GearsofWarEDay) Ar.SkipBulkArrayData();

        if (bUseHighPrecisionPerInstanceSMData)
        {
            PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar));
        }
        else
        {
            PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar.Read<FTransform>()));
        }

        LastInstanceBodyIndex = Ar.Game >= GAME_UE5_8 ? Ar.Read<int>() : 0;
        InstancingRandomSeed = Ar.Read<int>();
        PerInstanceSMCustomData = Ar.ReadBulkArray(Ar.Read<float>);
        AdditionalRandomSeeds = Ar.ReadArray<FInstancedStaticMeshRandomSeed>();
        if (Ar.Game is GAME_SilverPalace)
        {
            Ar.Position += 16;
            return;
        }
        NavigationBounds = new FBox(Ar);
        SceneProxyDesc.InstancedStaticMeshSceneProxyDesc = new FInstancedStaticMeshSceneProxyDesc(Ar);
        SpatialHashes = Ar.Game >= GAME_UE5_8 ? Ar.ReadBulkArray(() => new FCompressedSpatialHashItem(Ar)) : [];
        PerInstanceRandomIDs = Ar.Game >= GAME_UE5_8 ? Ar.ReadBulkArray<float>() : [] ;
    }
}

