using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Writers.ActorX.Structs;

namespace CUE4Parse.Tests.Conversion.Writers.ActorX;

public class CVertexShareTests
{
    [Fact]
    public void PointIndicesPreserveSourceWedgeMappingForDuplicatePositions()
    {
        var position = new FVector(10.0f, 20.0f, 30.0f);
        var vertices = new[]
        {
            new MeshVertex(position, new FVector(0.0f, 0.0f, 1.0f), FVector4.ZeroVector, FMeshUVFloat.ZeroVector),
            new MeshVertex(position, new FVector(0.0f, 1.0f, 0.0f), FVector4.ZeroVector, FMeshUVFloat.ZeroVector)
        };
        var share = new CVertexShare();
        share.Prepare(vertices);

        foreach (var vertex in vertices)
        {
            share.AddVertex(vertex.Position, vertex.Normal);
        }

        Assert.True(share.TryGetPointIndexForWedge(0, out var firstPointIndex));
        Assert.True(share.TryGetPointIndexForWedge(1, out var secondPointIndex));
        Assert.Equal(0, firstPointIndex);
        Assert.Equal(1, secondPointIndex);
        Assert.False(share.TryGetPointIndexForWedge(2, out _));
    }
}
