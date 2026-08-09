using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureRenderableAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void StaticMeshPreservesLodsSectionsVertexStreamsAndSocket(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var mesh = LoadExport<UStaticMesh>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Meshes/SM_Fixture.uasset",
            "SM_Fixture");

        Assert.True(mesh.bCooked);
        Assert.Equal(["Primary", "Secondary"],
            mesh.StaticMaterials.Select(material => material.MaterialSlotName.Text).ToArray());

        var lods = Assert.IsType<FStaticMeshLODResources[]>(mesh.RenderData?.LODs);
        Assert.Equal(2, lods.Length);
        AssertLod(lods[0], expectedVertices: 6, expectedTriangles: 2, expectedSections: 2);
        AssertLod(lods[1], expectedVertices: 6, expectedTriangles: 2, expectedSections: 2);

        Assert.Contains(lods[0].PositionVertexBuffer!.Verts,
            vertex => vertex.X == -50.0f && vertex.Y == -50.0f && vertex.Z == 0.0f);
        Assert.Contains(lods[0].PositionVertexBuffer!.Verts,
            vertex => vertex.X == 50.0f && vertex.Y == 50.0f && vertex.Z == 20.0f);
        var socketReference = Assert.Single(mesh.Sockets);
        var socket = Assert.IsType<UStaticMeshSocket>(socketReference.Load<UStaticMeshSocket>());
        Assert.Equal("FixtureSocket", socket.SocketName.Text);
        AssertVector(socket.RelativeLocation, 12.5f, -25.0f, 37.5f);
        Assert.Equal((10.0f, 20.0f, 30.0f),
            (socket.RelativeRotation.Pitch, socket.RelativeRotation.Yaw, socket.RelativeRotation.Roll));
        AssertVector(socket.RelativeScale, 1.25f, 0.75f, 2.0f);
        Assert.Equal("DeterministicFixtureSocket", socket.Tag);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MaterialInstancePreservesParentAndParameterOverrides(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var instance = LoadExport<UMaterialInstanceConstant>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Materials/MI_Fixture.uasset",
            "MI_Fixture");

        var parent = Assert.IsType<UMaterial>(instance.Parent);
        Assert.Equal("M_Fixture", parent.Name);
        Assert.Contains(parent.ReferencedTextures, texture => texture.Name == "T_BC3");

        var scalar = Assert.Single(instance.ScalarParameterValues);
        Assert.Equal("FixtureRoughness", scalar.Name);
        Assert.Equal(0.8125f, scalar.ParameterValue);

        var vector = Assert.Single(instance.VectorParameterValues);
        Assert.Equal("PrimaryColor", vector.Name);
        Assert.Equal((0.25f, 0.75f, 0.5f, 0.875f),
            (vector.ParameterValue!.Value.R, vector.ParameterValue.Value.G,
                vector.ParameterValue.Value.B, vector.ParameterValue.Value.A));

        var texture = Assert.Single(instance.TextureParameterValues);
        Assert.Equal("FixtureTexture", texture.Name);
        Assert.Equal("T_BC3", Assert.IsType<UTexture2D>(texture.ParameterValue.Load<UTexture2D>()).Name);

        var staticSwitch = Assert.Single(Assert.IsType<FStaticParameterSet>(instance.StaticParameters).StaticSwitchParameters);
        Assert.Equal("UseAlternateColor", staticSwitch.Name);
        Assert.True(staticSwitch.bOverride);
        Assert.True(staticSwitch.Value);
    }

    private static void AssertLod(
        FStaticMeshLODResources lod,
        int expectedVertices,
        int expectedTriangles,
        int expectedSections)
    {
        Assert.False(lod.SkipLod);
        Assert.Equal(expectedVertices, lod.PositionVertexBuffer!.NumVertices);
        Assert.Equal(expectedVertices, lod.PositionVertexBuffer.Verts.Length);
        Assert.Equal(expectedVertices, lod.VertexBuffer!.NumVertices);
        Assert.Equal(2, lod.VertexBuffer.NumTexCoords);
        Assert.Equal(expectedVertices, lod.VertexBuffer.UV.Length);
        Assert.All(lod.VertexBuffer.UV, vertex => Assert.Equal(2, vertex.UV.Length));
        Assert.Equal(expectedVertices, lod.ColorVertexBuffer!.NumVertices);
        Assert.Equal(expectedVertices, lod.ColorVertexBuffer.Data.Length);
        Assert.True(lod.IndexBuffer!.Buffer!.Length >= expectedTriangles * 3);
        Assert.Equal(0, lod.IndexBuffer.Buffer.Length % 3);

        var populatedSections = lod.Sections.Where(section => section.NumTriangles > 0).ToArray();
        Assert.Equal(expectedSections, populatedSections.Length);
        Assert.Equal(expectedTriangles, populatedSections.Sum(section => section.NumTriangles));
        Assert.Equal(Enumerable.Range(0, expectedSections),
            populatedSections.Select(section => section.MaterialIndex).Order());
    }
}
