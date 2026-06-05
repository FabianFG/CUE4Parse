using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Houdini;

public class UHoudiniStaticMesh : UObject
{
    public FVector[] VertexPositions;
    public FIntVector[] TriangleIndices;
    public FColor[] VertexInstanceColors;
    public FVector[] VertexInstanceNormals;
    public FVector[] VertexInstanceUTangents;
    public FVector[] VertexInstanceVTangents;
    public FVector2D[] VertexInstanceUVs;
    public int[] MaterialIDsPerTriangle;

    public bool bHasNormals;
    public bool bHasTangents;
    public bool bHasColors;
    public uint NumUVLayers;
    public bool bHasPerFaceMaterials;
    public FStaticMaterial[]? StaticMaterials;
    public ResolvedObject?[] Materials;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bHasNormals = GetOrDefault<bool>(nameof(bHasNormals));
        bHasTangents = GetOrDefault<bool>(nameof(bHasTangents));
        bHasColors = GetOrDefault<bool>(nameof(bHasColors));
        NumUVLayers = GetOrDefault<uint>(nameof(NumUVLayers));
        bHasPerFaceMaterials = GetOrDefault<bool>(nameof(bHasPerFaceMaterials));
        StaticMaterials = GetOrDefault(nameof(StaticMaterials), Array.Empty<FStaticMaterial>());

        VertexPositions = Ar.ReadBulkArray<FVector>();
        TriangleIndices = Ar.ReadBulkArray<FIntVector>();
        VertexInstanceColors = Ar.ReadBulkArray<FColor>();
        VertexInstanceNormals = Ar.ReadBulkArray<FVector>();
        VertexInstanceUTangents = Ar.ReadBulkArray<FVector>();
        VertexInstanceVTangents = Ar.ReadBulkArray<FVector>();
        VertexInstanceUVs = Ar.ReadBulkArray<FVector2D>();
        MaterialIDsPerTriangle = Ar.ReadBulkArray<int>();

        Materials = new ResolvedObject[StaticMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = StaticMaterials[i].MaterialInterface;
        }
    }
}
