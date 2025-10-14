using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.FF7.Assets.Objects;

public static class FF7FStaticLodModel
{
    public static void ReadFStaticLodModel(FAssetArchive Ar, bool bHasVertexColors, FByteBulkData bulk, out FMultisizeIndexContainer Indices,
        out FSkeletalMeshVertexBuffer VertexBufferGPUSkin, out FSkeletalMeshVertexColorBuffer ColorVertexBuffer, out int NumVertices, out int NumTexCoords)
    {
        var metaAr = new FByteArchive("MetaData", Ar.ReadBytes(161));

        var DataTypeSize = metaAr.Read<byte>();
        var CachedNumIndices = metaAr.Read<int>();
        var b32Bit = DataTypeSize == 4;

        var tempNumTexCoords = metaAr.Read<int>();
        var tempNumVertices = metaAr.Read<int>();
        var tempbUseFullPrecisionUVs = metaAr.ReadBoolean();
        var tempbUseHighPrecisionTangentBasis = metaAr.ReadBoolean();

        var PosStride = metaAr.Read<int>();
        var PosNumVertices = metaAr.Read<int>();

        var ColorStride = metaAr.Read<int>();
        var ColorNumVertices = metaAr.Read<int>();

        var bVariableBonesPerVertex = metaAr.ReadBoolean();
        var MaxBoneInfluences = metaAr.Read<int>();
        var NumBoneWeights = metaAr.Read<int>();
        var NumVertices2 = metaAr.Read<int>();
        var bUse16BitBoneIndex = metaAr.ReadBoolean();
        var NumVertices3 = metaAr.Read<int>(); // Lookup maybe

        var profileNamesMeta = metaAr.ReadArray(Ar.ReadFName);

        var PositionBufferOffset = metaAr.Read<int>();
        var PositionBufferSize = metaAr.Read<int>();

        var TangentsBufferOffset = metaAr.Read<int>();
        var TangentsBufferSize = metaAr.Read<int>();

        var UVBufferOffset = metaAr.Read<int>();
        var UVBufferSize = metaAr.Read<int>();

        metaAr.Position += 8;

        var skinWeightVertexBufferOffset = metaAr.Read<int>();
        var skinWeightVertexBufferSize = metaAr.Read<int>();

        var ColorVertexBufferOffset = metaAr.Read<int>();
        var ColorVertexBufferSize = metaAr.Read<int>();

        metaAr.Position += 24; // Bonamik or KDI buffers

        var IndicesBufferOffset = metaAr.Read<int>();
        var IndicesBufferSize = metaAr.Read<int>();

        using (var tempAr = new FByteArchive("FF7LodReader", bulk.Data, Ar.Versions))
        {
            VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer()
            {
                bUseFullPrecisionUVs = true,
                NumTexCoords = tempNumTexCoords,
                bExtraBoneInfluences = MaxBoneInfluences > 4,
            };

            Indices = new FMultisizeIndexContainer();
            tempAr.Position = IndicesBufferOffset;
            if (b32Bit)
            {
                Indices.SetIndices(tempAr.ReadArray<uint>(CachedNumIndices));
            }
            else
            {
                Indices.SetIndices(tempAr.ReadArray<ushort>(CachedNumIndices));
            }

            tempAr.Position = PositionBufferOffset;
            var verts = tempAr.ReadArray<FVector>(tempNumVertices);

            tempAr.Position = TangentsBufferOffset;
            var normals = tempAr.ReadArray(tempNumVertices, () => FStaticMeshUVItem.SerializeTangents(tempAr, tempbUseHighPrecisionTangentBasis));
            tempAr.Position = UVBufferOffset;
            var uvs = tempAr.ReadArray(tempNumVertices, () => FStaticMeshUVItem.SerializeTexcoords(tempAr, tempNumTexCoords, tempbUseFullPrecisionUVs));

            tempAr.Position = skinWeightVertexBufferOffset;
            var weights = tempAr.ReadArray(NumVertices2, () => new FSkinWeightInfo(tempAr, MaxBoneInfluences > 4, bUse16BitBoneIndex));

            if (bHasVertexColors && ColorVertexBufferOffset != -1)
            {
                tempAr.Position = ColorVertexBufferOffset;
                ColorVertexBuffer = new FSkeletalMeshVertexColorBuffer(tempAr.ReadArray<FColor>(ColorNumVertices));
            }
            else
            {
                ColorVertexBuffer = new FSkeletalMeshVertexColorBuffer();
            }

            NumVertices = tempNumVertices;
            NumTexCoords = tempNumTexCoords;

            VertexBufferGPUSkin.VertsFloat = new FGPUVertFloat[NumVertices];
            for (var i = 0; i < VertexBufferGPUSkin.VertsFloat.Length; i++)
            {
                VertexBufferGPUSkin.VertsFloat[i] = new FGPUVertFloat
                {
                    Pos = verts[i],
                    Infs = weights[i],
                    Normal = normals[i],
                    UV = uvs[i]
                };
            }
        }
    }
}
