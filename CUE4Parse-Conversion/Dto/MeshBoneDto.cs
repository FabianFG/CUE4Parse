using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Dto;

public readonly struct MeshBoneDto(string name, int parentIndex, FTransform transform)
{
    public readonly string Name = name;
    public readonly int ParentIndex = parentIndex;
    public readonly FTransform Transform = transform;

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
