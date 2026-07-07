using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[JsonConverter(typeof(FSkelMeshChunkConverter))]
public class FSkelMeshChunk
{
    public readonly int BaseVertexIndex;
    public readonly FRigidVertex[]? RigidVertices;
    public readonly FSoftVertex[]? SoftVertices;
    public readonly ushort[] BoneMap;
    public readonly int NumRigidVertices;
    public readonly int NumSoftVertices;
    public readonly int MaxBoneInfluences;
    public readonly bool HasClothData;

    public FSkelMeshChunk(FArchive Ar)
    {
        var stripDataFlags = new FStripDataFlags(Ar);

        if (!stripDataFlags.IsAudioVisualDataStripped())
            BaseVertexIndex = Ar.Read<int>();

        if (!stripDataFlags.IsEditorDataStripped())
        {
            RigidVertices = Ar.ReadArray(() => new FRigidVertex(Ar));
            SoftVertices = Ar.ReadArray(() => new FSoftVertex(Ar));
        }

        BoneMap = Ar.ReadArray<ushort>();
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.USE_UMA_RESOURCE_ARRAY_MESH_DATA)
        {
            NumRigidVertices = Ar.Read<int>();
            NumSoftVertices = Ar.Read<int>();
        }
        else
        {
            NumRigidVertices = RigidVertices!.Length;
            NumSoftVertices = SoftVertices!.Length;
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.GPUSKIN_MAX_INFLUENCES_OPTIMIZATION)
        {
            MaxBoneInfluences = Ar.Read<int>();
        }
        HasClothData = false;

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.APEX_CLOTH)
        {
            // Physics data, drop
            var clothMappingData = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
            Ar.ReadArray(() => new FVector(Ar)); // PhysicalMeshVertices
            Ar.ReadArray(() => new FVector(Ar)); // PhysicalMeshNormals
            Ar.Position += 4; // CorrespondClothAssetIndex, ClothAssetSubmeshIndex
            HasClothData = clothMappingData.Length > 0;
        }
    }
}
