using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[JsonConverter(typeof(FSkeletalMeshVertexBufferConverter))]
public class FSkeletalMeshVertexBuffer
{
    public int NumTexCoords;
    public FVector MeshExtension;
    public FVector MeshOrigin;
    public bool bUseFullPrecisionUVs;
    public bool bExtraBoneInfluences;
    public FGPUVertHalf[] VertsHalf;
    public FGPUVertFloat[] VertsFloat;

    public FSkeletalMeshVertexBuffer()
    {
        VertsHalf = [];
        VertsFloat = [];
    }

    public FSkeletalMeshVertexBuffer(FAssetArchive Ar) : this()
    {
        var stripDataFlags = new FStripDataFlags(Ar, FPackageFileVersion.CreateUE4Version(EUnrealEngineObjectUE4Version.STATIC_SKELETAL_MESH_SERIALIZATION_FIX));

        NumTexCoords = Ar.Read<int>();
        bUseFullPrecisionUVs = Ar.ReadBoolean();

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SUPPORT_GPUSKINNING_8_BONE_INFLUENCES &&
            FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.UseSeparateSkinWeightBuffer)
        {
            bExtraBoneInfluences = Ar.ReadBoolean();
        }

        MeshExtension = new FVector(Ar);
        MeshOrigin = new FVector(Ar);

        if (!bUseFullPrecisionUVs)
            VertsHalf = Ar.ReadBulkArray(() => new FGPUVertHalf(Ar, bExtraBoneInfluences, NumTexCoords));
        else
            VertsFloat = Ar.ReadBulkArray(() => new FGPUVertFloat(Ar, bExtraBoneInfluences, NumTexCoords));
    }

    public int GetVertexCount()
    {
        if (VertsHalf.Length > 0) return VertsHalf.Length;
        if (VertsFloat.Length > 0) return VertsFloat.Length;
        return 0;
    }
}
