using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics;

public class FPhysicsBody
{
    [JsonIgnore] public int Version = 4;
    public int CustomId;
    public FPhysicsBodyAggregate[] Bodies;
    public FBoneName[] BoneIds;
    public ushort[] BoneIds_DEPRECATED = [];
    public string[] BonesNames_Deprecated = [];
    public int[] BodiesCustomIds;
    public bool bBodiesModified;
    
    public FPhysicsBody(FMutableArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_6) Version = Ar.Read<int>();
        if (Version >= 2)
            CustomId = Ar.Read<int>();

        Bodies = Ar.ReadArray(() => new FPhysicsBodyAggregate(Ar));
        if (Version >= 4)
        {
            BoneIds = Ar.ReadArray<FBoneName>();
        }
        else if (Version >= 3)
        {
            BoneIds_DEPRECATED = Ar.ReadArray<ushort>();
        }
        else
        {
            BonesNames_Deprecated = Ar.ReadArray(Ar.ReadString);
        }
            
        BodiesCustomIds = Ar.ReadArray<int>();
        if (Version >= 1)
            bBodiesModified = Ar.ReadFlag();
    }
}
