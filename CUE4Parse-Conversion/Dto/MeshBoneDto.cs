using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Dto;

public readonly struct MeshBoneDto
{
    public readonly string Name;
    public readonly int ParentIndex;
    public readonly FTransform Transform;

    public MeshBoneDto(string name, int parentIndex, FTransform transform)
    {
        Name = name;
        ParentIndex = parentIndex;

        if (ParentIndex < 0)
        {
            // root bone scaling offsets all other bones, and even tho it makes no sense, some games do it anyway, so just get rid of it
            transform.SetScale3D(FVector.OneVector);
        }
        Transform = transform;
    }

    public MeshBoneDto(FMeshBoneInfo info, FTransform transform) : this(info.Name.Text, info.ParentIndex, transform)
    {

    }

    public MeshBoneDto(USkeletalMeshSocket socket, int parentIndex) : this(socket.SocketName.Text, parentIndex, new FTransform(socket.RelativeRotation.Quaternion(), socket.RelativeLocation, socket.RelativeScale))
    {

    }

    public MeshBoneDto(UStaticMeshSocket socket) : this(socket.SocketName.Text, -1, new FTransform(socket.RelativeRotation.Quaternion(), socket.RelativeLocation, socket.RelativeScale))
    {

    }
}
