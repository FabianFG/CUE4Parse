using System;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeletons;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics;

public class PhysicsBody : IMutablePtr
{
    public int Version;
    public int CustomId;
    public FPhysicsBodyAggregate[] Bodies;
    public FBoneName[] BoneIds;
    public int[] BodiesCustomIds;
    public bool bBodiesModified;
    
    public bool IsBroken { get; set; }

    public PhysicsBody(FAssetArchive Ar)
    {
        Version = Ar.Read<int>();

        if (Version == -1)
        {
            IsBroken = true;
            return;
        }
        
        if (Version > 4)
            throw new NotSupportedException($"Mutable PhysicsBody Version '{Version}' is currently not supported");

        if (Version >= 2)
        {
            CustomId = Ar.Read<int>();
        }

        Bodies = Ar.ReadArray(() => new FPhysicsBodyAggregate(Ar));

        if (Version >= 4)
        {
            BoneIds = Ar.ReadArray(() => new FBoneName(Ar));
        }
        else if (Version == 3)
        {
            var boneIds_DEPRECATED = Ar.ReadArray<ushort>();

            var numBoneNames = boneIds_DEPRECATED.Length;
            BoneIds = new FBoneName[numBoneNames];
            for (int i = 0; i < numBoneNames; i++)
            {
                BoneIds[i].Id = boneIds_DEPRECATED[i];
            }
        }
        else
        {
            var boneNames = Ar.ReadArray(Ar.ReadMutableFString);
            
            var numBoneNames = boneNames.Length;
            
            string[] uniqueBoneNames = new string[numBoneNames];
            BoneIds = new FBoneName[numBoneNames];

            for (uint i = 0; i < numBoneNames; i++)
            {
                BoneIds[i]= new FBoneName(i);
            }
        }

        BodiesCustomIds = Ar.ReadArray<int>();

        if (Version >= 1)
        {
            bBodiesModified = Ar.ReadFlag();
        }
    }
}