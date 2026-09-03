using System.Text;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse_Conversion.Options;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureActorXExportTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void StaticMeshPreservesOriginalVertexIndices(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var mesh = LoadExport<UStaticMesh>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Meshes/SM_Fixture.uasset",
            "SM_Fixture");
        using var converted = new StaticMeshDto(mesh);
        var lod = converted.LODs[0];
        var uniquePositionCount = lod.Vertices
            .Select(vertex => (vertex.Position.X, vertex.Position.Y, vertex.Position.Z))
            .Distinct()
            .Count();
        Assert.True(uniquePositionCount < lod.Vertices.Length);

        var exports = new ActorXMeshFormat().BuildStaticMesh(
            mesh.Name,
            new ExportOptions(meshFormat: EMeshFormat.ActorX),
            converted);
        var export = Assert.Single(exports, file => string.IsNullOrEmpty(file.NameSuffix));

        AssertOriginalVertexIndices(export.Data, lod.Vertices.Length);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void SkeletalMeshPreservesOriginalVertexWeights(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var mesh = LoadExport<USkeletalMesh>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Meshes/SK_Fixture.uasset",
            "SK_Fixture");
        using var converted = new SkeletalMeshDto(mesh);
        var lod = converted.LODs[0];
        var exports = new ActorXMeshFormat().BuildSkeletalMesh(
            mesh.Name,
            new ExportOptions(meshFormat: EMeshFormat.ActorX),
            converted);
        var export = Assert.Single(exports, file => string.IsNullOrEmpty(file.NameSuffix));

        AssertOriginalVertexIndices(export.Data, lod.Vertices.Length);

        var weights = FindChunk(export.Data, "RAWWEIGHTS");
        Assert.Equal(lod.Vertices.Sum(vertex => vertex.Influences.Length), weights.DataCount);
        var influencesPerPoint = new int[lod.Vertices.Length];
        using var reader = new BinaryReader(new MemoryStream(export.Data));
        for (var influenceIndex = 0; influenceIndex < weights.DataCount; influenceIndex++)
        {
            reader.BaseStream.Position = weights.DataOffset + influenceIndex * weights.DataSize + sizeof(float);
            var pointIndex = reader.ReadInt32();
            Assert.InRange(pointIndex, 0, lod.Vertices.Length - 1);
            influencesPerPoint[pointIndex]++;
        }

        Assert.Equal(lod.Vertices.Select(vertex => vertex.Influences.Length), influencesPerPoint);
    }

    private static void AssertOriginalVertexIndices(byte[] data, int vertexCount)
    {
        var points = FindChunk(data, "PNTS0000");
        Assert.Equal(vertexCount, points.DataCount);

        var wedges = FindChunk(data, "VTXW0000");
        Assert.Equal(vertexCount, wedges.DataCount);
        using var reader = new BinaryReader(new MemoryStream(data));
        for (var wedgeIndex = 0; wedgeIndex < wedges.DataCount; wedgeIndex++)
        {
            reader.BaseStream.Position = wedges.DataOffset + wedgeIndex * wedges.DataSize;
            Assert.Equal(wedgeIndex, reader.ReadInt32());
        }
    }

    private static ActorXChunk FindChunk(byte[] data, string expectedName)
    {
        using var reader = new BinaryReader(new MemoryStream(data));
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var name = Encoding.ASCII.GetString(reader.ReadBytes(20)).TrimEnd('\0');
            reader.ReadInt32(); // TypeFlag
            var dataSize = reader.ReadInt32();
            var dataCount = reader.ReadInt32();
            var dataOffset = reader.BaseStream.Position;
            if (name == expectedName)
            {
                return new ActorXChunk(dataOffset, dataSize, dataCount);
            }

            reader.BaseStream.Position += (long) dataSize * dataCount;
        }

        throw new InvalidDataException($"ActorX chunk '{expectedName}' was not found.");
    }

    private readonly record struct ActorXChunk(long DataOffset, int DataSize, int DataCount);
}
