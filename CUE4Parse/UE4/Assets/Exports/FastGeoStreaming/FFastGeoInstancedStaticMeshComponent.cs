using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;

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
            >= EGame.GAME_UE5_8 => Ar.ReadBoolean(),
            _ => true,
        };

        if (bUseHighPrecisionPerInstanceSMData)
        {
            PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar));
        }
        else
        {
            PerInstanceSMData = Ar.ReadBulkArray(() => new FInstancedStaticMeshInstanceData(Ar.Read<FTransform>()));
        }
        
        LastInstanceBodyIndex = Ar.Game >= EGame.GAME_UE5_8 ? Ar.Read<int>() : 0;
        InstancingRandomSeed = Ar.Read<int>();
        PerInstanceSMCustomData = Ar.ReadBulkArray(Ar.Read<float>);
        AdditionalRandomSeeds = Ar.ReadArray<FInstancedStaticMeshRandomSeed>();
        NavigationBounds = new FBox(Ar);
        SceneProxyDesc.InstancedStaticMeshSceneProxyDesc = new FInstancedStaticMeshSceneProxyDesc(Ar);
        SpatialHashes = Ar.Game >= EGame.GAME_UE5_8 ? Ar.ReadBulkArray<FCompressedSpatialHashItem>() : [] ;
        PerInstanceRandomIDs = Ar.Game >= EGame.GAME_UE5_8 ? Ar.ReadBulkArray<float>() : [] ;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FCompressedSpatialHashItem
{
    //using FLocation64 = TLocation<int64>;
    public TLocation<long> Location;
    public int NumInstances;
}

[StructLayout(LayoutKind.Sequential)]
public struct TLocation<T>
{
    public TIntVector3<T> Coord;
    public int Level;
}
