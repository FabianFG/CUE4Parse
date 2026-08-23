using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

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

        var bodySetup = Assert.IsType<UBodySetup>(mesh.BodySetup.Load<UBodySetup>());
        Assert.True(bodySetup.BodySetupGuid.IsValid());
        var aggregate = Assert.IsType<FKAggregateGeom>(bodySetup.AggGeom);
        Assert.True(
            aggregate.SphereElems.Length + aggregate.BoxElems.Length + aggregate.SphylElems.Length +
            aggregate.ConvexElems.Length + aggregate.TaperedCapsuleElems.Length > 0);
        Assert.NotNull(bodySetup.CookedFormatData);
        Assert.NotEmpty(bodySetup.CookedFormatData.Formats);
        Assert.All(bodySetup.CookedFormatData.Formats.Values, data => Assert.True(data.GetDataSize() > 0));
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

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void BaseMaterialPreservesCachedParametersAndAvailableShaderResources(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        provider.ReadShaderMaps = true;
        var material = LoadExport<UMaterial>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Materials/M_Fixture.uasset",
            "M_Fixture");

        var parameters = new CMaterialParams2();
        material.GetParams(parameters, EMaterialDepth.TopLayerOnly);

        Assert.Equal(0.375f, parameters.Scalars["FixtureRoughness"]);
        Assert.Equal((0.1f, 0.2f, 0.8f, 1.0f),
            (parameters.Colors["PrimaryColor"].R, parameters.Colors["PrimaryColor"].G,
                parameters.Colors["PrimaryColor"].B, parameters.Colors["PrimaryColor"].A));
        Assert.Equal((0.9f, 0.15f, 0.05f, 1.0f),
            (parameters.Colors["AlternateColor"].R, parameters.Colors["AlternateColor"].G,
                parameters.Colors["AlternateColor"].B, parameters.Colors["AlternateColor"].A));
        Assert.Equal("T_BC3", Assert.IsType<UTexture2D>(parameters.Textures["FixtureTexture"]).Name);

        // The minimal UE6 repack intentionally contains no inline or library shader payloads.
        if (FixtureGame < EGame.GAME_UE6_0)
        {
            Assert.NotEmpty(material.LoadedMaterialResources);
            Assert.All(material.LoadedMaterialResources, resource =>
            {
                var shaderMap = Assert.IsType<FMaterialShaderMap>(resource.LoadedShaderMap);
                Assert.NotEmpty(shaderMap.FrozenArchive.FrozenObject);
                Assert.True(shaderMap.ResourceHash is not null || shaderMap.Code is not null);
            });
        }
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void NaniteStaticMeshPreservesResourcesAndDecodablePages(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        provider.ReadNaniteData = true;
        var mesh = LoadExport<UStaticMesh>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Meshes/SM_Nanite.uasset",
            "SM_Nanite");

        Assert.True(mesh.bCooked);
        Assert.Equal("NaniteSurface", Assert.Single(mesh.StaticMaterials).MaterialSlotName.Text);
        var resources = Assert.IsType<FNaniteResources>(mesh.RenderData?.NaniteResources);
        Assert.Equal(2_048u, resources.NumInputTriangles);
        Assert.Equal(1_089u, resources.NumInputVertices);
        Assert.NotEqual(0u, resources.NumClusters);
        Assert.NotEmpty(resources.PageStreamingStates);

        resources.LoadAllPages();
        try
        {
            var pages = resources.LoadedPages
                .OfType<FNaniteStreamableData>()
                .ToArray();
            Assert.NotEmpty(pages);
            Assert.All(pages, page =>
            {
                Assert.Equal(page.NumClusters, page.Clusters.Length);
                Assert.NotEmpty(page.Clusters);
            });
        }
        finally
        {
            resources.UnloadAllPages();
        }

        using var converted = new StaticMeshDto(mesh, naniteFormat: ENaniteMeshFormat.NaniteOnly);
        var lod = Assert.Single(converted.LODs);
        Assert.True(lod.IsNanite);
        Assert.NotEmpty(lod.Vertices);
        Assert.NotEmpty(lod.Indices);
        Assert.Equal(0, lod.Indices.Length % 3);
        Assert.NotEmpty(lod.Sections);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void SkeletalMeshPreservesBonesLodsSkinWeightsAndMorphTarget(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var mesh = LoadExport<USkeletalMesh>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Meshes/SK_Fixture.uasset",
            "SK_Fixture");

        AssertReferenceSkeleton(mesh.ReferenceSkeleton);
        Assert.True(mesh.bHasVertexColors);
        Assert.True(mesh.Skeleton.TryLoad<USkeleton>(out var skeleton));
        Assert.Equal("SKEL_Fixture", skeleton.Name);

        var lods = Assert.IsType<FStaticLODModel[]>(mesh.LODModels);
        Assert.Equal([6, 3], lods.Select(lod => lod.NumVertices).ToArray());
        Assert.All(lods, lod =>
        {
            Assert.False(lod.SkipLod);
            Assert.NotEmpty(lod.Sections);
            Assert.NotEmpty(lod.Indices!.Buffer!);
            Assert.Equal(lod.NumVertices, lod.VertexBufferGPUSkin.VertsFloat.Length);
        });

        var morph = Assert.IsType<UMorphTarget>(Assert.Single(mesh.MorphTargets).Load<UMorphTarget>());
        Assert.Equal("Morph_Fixture", morph.Name);
        var morphLod = Assert.Single(morph.MorphLODModels);
        Assert.Equal(2, morphLod.NumBaseMeshVerts);
        Assert.Equal([0], morphLod.SectionIndices);
        Assert.False(morphLod.bGeneratedByEngine);
        // Win64 uses the cooked GPU morph buffers, so UE strips the raw CPU delta array.
        Assert.Empty(morphLod.Vertices);

        using var converted = new SkeletalMeshDto(mesh);
        Assert.Equal(3, converted.Bones.Length);
        Assert.Equal(2, converted.LODs.Count);
        Assert.All(converted.LODs, lod =>
        {
            Assert.NotEmpty(lod.Vertices);
            Assert.NotEmpty(lod.Indices);
            Assert.Equal(0, lod.Indices.Length % 3);
        });
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void GeometryCollectionPreservesManagedArraysAndFallbackRenderData(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var asset = LoadExport<UGeometryCollection>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Geometry/GC_Fixture.uasset",
            "GC_Fixture");

        Assert.Equal(2, asset.Materials.Length);
        var collection = Assert.IsType<FGeometryCollection>(asset.GeometryCollection);
        Assert.Equal(1, collection.GroupInfo.Single(group => group.Key.Text == "Transform").Value.Size);
        Assert.Equal(6, collection.GroupInfo.Single(group => group.Key.Text == "Vertices").Value.Size);
        Assert.Equal(2, collection.GroupInfo.Single(group => group.Key.Text == "Faces").Value.Size);
        Assert.Equal(1, collection.GroupInfo.Single(group => group.Key.Text == "Geometry").Value.Size);
        Assert.NotEmpty(collection.Map);

        var renderData = Assert.IsType<FGeometryCollectionRenderData>(asset.RenderData);
        Assert.True(renderData.bHasMeshData);
        Assert.False(renderData.bHasNaniteData);
        Assert.NotNull(renderData.MeshResources);
        Assert.NotNull(renderData.MeshDescription);
        Assert.NotNull(renderData.PreSkinnedBounds);
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
