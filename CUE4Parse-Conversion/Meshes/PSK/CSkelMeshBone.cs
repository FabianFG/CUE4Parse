using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class CSkelMeshBone(FName name, int parentIndex, FVector position, FQuat orientation, FVector scale) : ICloneable
{
    public readonly FName Name = name;
    public readonly int ParentIndex = parentIndex;
    public readonly FVector Position = position;
    public readonly FQuat Orientation = orientation;
    public readonly FVector Scale = scale;

    public CSkelMeshBone(FMeshBoneInfo info, FTransform transform) : this(info.Name, info.ParentIndex, transform.Translation, transform.Rotation, transform.Scale3D)
    {

    }

    public CSkelMeshBone(USkeletalMeshSocket socket, int parentIndex) : this(socket.SocketName, parentIndex, socket.RelativeLocation, socket.RelativeRotation.Quaternion(), socket.RelativeScale)
    {

    }

    public CSkelMeshBone(UStaticMeshSocket socket) : this(socket.SocketName, -1, socket.RelativeLocation, socket.RelativeRotation.Quaternion(), socket.RelativeScale)
    {

    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
