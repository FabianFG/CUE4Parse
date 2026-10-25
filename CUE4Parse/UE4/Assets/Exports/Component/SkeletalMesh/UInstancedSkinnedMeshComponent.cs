using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.InstanceData;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

public class UInstancedSkinnedMeshComponent : USkinnedMeshComponent
{
    public FSkinnedMeshInstanceData[] InstanceData = [];
    public float[] InstanceCustomData = [];
    public FInstanceDataManager? InstanceDataManager;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        var bCooked = Ar.ReadBoolean();
        var bHasSkipSerializationPropertiesData = Ar.ReadBoolean();
        if (bHasSkipSerializationPropertiesData)
        {
            // Read existing data if it was serialized
            if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.SkinnedMeshInstanceDataSerializationV2)
            {
                var tempInstanceData_Deprecated = Ar.ReadBulkArray<FSkinnedMeshInstanceData_Deprecated>();
                InstanceData = new FSkinnedMeshInstanceData[tempInstanceData_Deprecated.Length];
                for (var i = 0; i < InstanceData.Length; i++)
                {
                    var elem = tempInstanceData_Deprecated[i];
                    InstanceData[i].Transform.SetFromMatrix(elem.Transform);
                    InstanceData[i].AnimationIndex = elem.AnimationIndex;
                }
            }
            else
            {
                InstanceData = Ar.ReadArray<FSkinnedMeshInstanceData>();
            }
            InstanceCustomData = Ar.ReadBulkArray<float>();

            if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.SkinnedMeshInstanceDataSerializationV2)
            {
                InstanceDataManager = new FInstanceDataManager(Ar);
            }

            if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.InstancedSkinnedMeshBoneAttachments)
            {
                LoadBoneAttachments(Ar);
            }

            if (bCooked)
            {
                InstanceDataManager?.ReadCookedRenderData(Ar);
            }
        }
    }

    public void LoadBoneAttachments(FAssetArchive Ar)
    {
        var sockets = Ar.ReadArray(() => (new FPackageIndex(Ar), Ar.ReadFName(), Ar.Read<int>()));
        var BoneAttachmentBindings = Ar.ReadArray<FBoneAttachmentBinding>();
    }

    public readonly struct FBoneAttachmentBinding
    {
        private readonly uint Bits;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(InstanceData));
        serializer.Serialize(writer, InstanceData);
        writer.WritePropertyName(nameof(InstanceCustomData));
        serializer.Serialize(writer, InstanceCustomData);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FSkinnedMeshInstanceData
{
    public FTransform Transform;
    public uint AnimationIndex;
}

public struct FSkinnedMeshInstanceData_Deprecated
{
    public FMatrix Transform;
    public uint AnimationIndex;
    private InlineArray3<uint> _padding;
}
