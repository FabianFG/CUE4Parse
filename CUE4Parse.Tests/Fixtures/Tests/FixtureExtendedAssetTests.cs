using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;
using CUE4Parse.UE4.Assets.Exports.AudioSynesthesia;
using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.ComputerFramework;
using CUE4Parse.UE4.Assets.Exports.GeometryCache;
using CUE4Parse.UE4.Assets.Exports.Harmonix;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureExtendedAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void UserDefinedEnumAndStructDeserializeSchemaAndDefaults(
        FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var enumAsset = LoadExport<UUserDefinedEnum>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Blueprints/E_Fixture.uasset", "E_Fixture");
        var enumValues = enumAsset.Names
            .Where(static entry => !entry.Item1.Text.EndsWith("_MAX", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal([0L, 1L, 2L], enumValues.Select(static entry => entry.Item2));
        Assert.All(enumValues, static entry => Assert.Contains("NewEnumerator", entry.Item1.Text));

        var structAsset = LoadExport<UUserDefinedStruct>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Blueprints/S_Fixture.uasset", "S_Fixture");
        Assert.Equal(EUserDefinedStructureStatus.UDSS_UpToDate, structAsset.Status);
        Assert.NotNull(structAsset.DefaultProperties);
        var defaults = structAsset.DefaultProperties.ToDictionary(
            static property => property.Name.Text.Split('_')[0],
            static property => property.Tag!);
        Assert.Contains("Count", defaults.Keys);
        Assert.Contains("Label", defaults.Keys);
        Assert.Contains("Offset", defaults.Keys);
        Assert.All(defaults.Values, Assert.NotNull);
        Assert.Equal(1337, defaults["Count"].GetValue<int>());
        Assert.Equal("Fixture user-defined string", defaults["Label"].GetValue<string>());
        var offset = defaults["Offset"].GetValue<FVector>();
        Assert.Equal((1.25d, -2.5d, 3.75d), (offset.X, offset.Y, offset.Z));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void TraditionalLevelStreamingAndSceneAttachmentsDeserialize(
        FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(
            provider, "CUE4ParseFixtures/Content/Fixtures/Maps/Streaming.umap");
        var world = Assert.Single(exports.OfType<UWorld>());
        var streaming = Assert.IsType<ULevelStreamingAlwaysLoaded>(
            Assert.Single(world.StreamingLevels).Load());
        Assert.Equal("/Game/Fixtures/Maps/Streaming_Sublevel.Streaming_Sublevel",
            streaming.WorldAsset?.AssetPathName.Text);
        var levelTransform = streaming.Get<FTransform>("LevelTransform");
        Assert.Equal((1000d, 2000d, 3000d),
            (levelTransform.Translation.X, levelTransform.Translation.Y, levelTransform.Translation.Z));
        Assert.Equal((2d, 2d, 2d),
            (levelTransform.Scale3D.X, levelTransform.Scale3D.Y, levelTransform.Scale3D.Z));

        var child = Assert.Single(exports.OfType<USceneComponent>(),
            component => component.Name == "AttachmentChild");
        Assert.Equal("AttachmentRoot", child.GetAttachParent()?.Name);
        Assert.Equal("FixtureSocket", child.Get<FName>("AttachSocketName").Text);
        Assert.Equal((11d, 22d, 33d),
            (child.RelativeLocation.X, child.RelativeLocation.Y, child.RelativeLocation.Z));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PhysicsAssetDeserializesBodiesConstraintsAndCollisionPairs(
        FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var physicsAsset = LoadExport<UPhysicsAsset>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Physics/PHYS_Fixture.uasset", "PHYS_Fixture");

        Assert.Equal([0, 1], physicsAsset.BoundsBodies);
        var bodies = physicsAsset.SkeletalBodySetups
            .Select(static index => Assert.IsType<USkeletalBodySetup>(index.Load()))
            .ToArray();
        Assert.Equal(["Root", "Joint_1"], bodies.Select(static body => body.BoneName.Text));

        var rootAggGeom = bodies[0].AggGeom;
        Assert.NotNull(rootAggGeom);
        var rootBox = Assert.Single(rootAggGeom.BoxElems);
        Assert.Equal((1d, 2d, 3d), (rootBox.Center.X, rootBox.Center.Y, rootBox.Center.Z));
        Assert.Equal((40f, 30f, 20f), (rootBox.X, rootBox.Y, rootBox.Z));
        var jointAggGeom = bodies[1].AggGeom;
        Assert.NotNull(jointAggGeom);
        var jointSphere = Assert.Single(jointAggGeom.SphereElems);
        Assert.Equal((-4d, 5d, 6d),
            (jointSphere.Center.X, jointSphere.Center.Y, jointSphere.Center.Z));
        Assert.Equal(12.5f, jointSphere.Radius);

        var constraint = Assert.IsType<UPhysicsConstraintTemplate>(
            Assert.Single(physicsAsset.ConstraintSetup).Load());
        Assert.Equal("RootToJoint", constraint.DefaultInstance.JointName.Text);
        Assert.Equal("Joint_1", constraint.DefaultInstance.ConstraintBone1.Text);
        Assert.Equal("Root", constraint.DefaultInstance.ConstraintBone2.Text);
        Assert.True(constraint.DefaultInstance.ProfileInstance.bDisableCollision);

        Assert.NotNull(physicsAsset.CollisionDisableTable);
        var collisionPair = Assert.Single(physicsAsset.CollisionDisableTable);
        Assert.Equal([0, 1], collisionPair.Key.Indices);
        Assert.False(collisionPair.Value);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void InstancedMeshComponentsDeserializeInstancesCustomDataAndClusterTree(
        FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(
            provider, "CUE4ParseFixtures/Content/Fixtures/Maps/Instancing.umap");

        var ism = Assert.Single(exports.OfType<UInstancedStaticMeshComponent>(),
            component => component.Name == "ISM_Fixture");
        Assert.Equal(4, ism.GetInstances().Length);
        Assert.NotNull(ism.PerInstanceSMCustomData);
        Assert.Equal([10f, 20f, 11f, 22f, 12f, 24f, 13f, 26f], ism.PerInstanceSMCustomData);

        var hism = Assert.Single(exports.OfType<UHierarchicalInstancedStaticMeshComponent>(),
            component => component.Name == "HISM_Fixture");
        Assert.Equal(12, hism.GetInstances().Length);
        Assert.NotNull(hism.PerInstanceSMCustomData);
        Assert.Equal(Enumerable.Range(100, 12).Select(static value => (float) value),
            hism.PerInstanceSMCustomData);
        Assert.NotEmpty(hism.ClusterTree ?? []);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MapBuildDataRegistryDeserializesDeterministicBuildData(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var registry = LoadExport<UMapBuildDataRegistry>(provider,
            "CUE4ParseFixtures/Content/Fixtures/BuildData/BuildData_Fixture.uasset",
            "BuildData_Fixture");

        Assert.NotNull(registry.MeshBuildData);
        var mesh = Assert.Single(registry.MeshBuildData).Value;
        Assert.Equal(2, mesh.IrrelevantLights.Length);
        var instance = Assert.Single(mesh.PerInstanceLightmapData);
        Assert.Equal((0.125f, 0.25f), (instance.LightmapUVBias.X, instance.LightmapUVBias.Y));
        Assert.Equal((0.5f, 0.75f), (instance.ShadowmapUVBias.X, instance.ShadowmapUVBias.Y));

        Assert.NotNull(registry.LightBuildData);
        var light = Assert.Single(registry.LightBuildData).Value;
        Assert.Equal(2, light.ShadowMapChannel);
        Assert.Equal((2, 2), (light.DepthMap.ShadowMapSizeX, light.DepthMap.ShadowMapSizeY));
        Assert.Equal(4, light.DepthMap.DepthSamples.Length);

        Assert.NotNull(registry.ReflectionCaptureBuildData);
        var capture = Assert.Single(registry.ReflectionCaptureBuildData).Value;
        Assert.Equal(1, capture.CubemapSize);
        Assert.Equal(0.625f, capture.AverageBrightness);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PoseSearchDatabaseDeserializesCookedSearchIndex(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var database = LoadExport<UPoseSearchDatabase>(provider,
            "CUE4ParseFixtures/Content/Fixtures/PoseSearch/PSD_Fixture.uasset", "PSD_Fixture");
        var searchIndex = Assert.IsType<FSearchIndex>(database.SearchIndexPrivate);
        Assert.NotEmpty(searchIndex.Assets);
        Assert.Equal(["Fixture", "Deterministic"],
            database.Get<FName[]>("Tags").Select(static tag => tag.Text));
        Assert.Equal("PS_Schema", database.Get<FPackageIndex>("Schema").Name);
        var schema = provider.LoadPackageObject<UObject>(
            "/Game/Fixtures/PoseSearch/PS_Schema.PS_Schema");
        var curveChannel = Assert.IsAssignableFrom<UObject>(
            Assert.Single(schema.Get<FPackageIndex[]>("Channels")).Load());
        Assert.Equal("FixtureCurve", curveChannel.Get<FName>("CurveName").Text);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void AudioAnalysisAssetsDeserializeCookedResults(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var loudness = LoadExport<ULoudnessNRT>(provider,
            "CUE4ParseFixtures/Content/Fixtures/AudioAnalysis/Loudness_Fixture.uasset",
            "Loudness_Fixture");
        Assert.True(loudness.DurationInSeconds > 0);
        Assert.NotEmpty(loudness.ChannelLoudnessArrays);

        var constantQ = LoadExport<UConstantQNRT>(provider,
            "CUE4ParseFixtures/Content/Fixtures/AudioAnalysis/ConstantQ_Fixture.uasset",
            "ConstantQ_Fixture");
        Assert.True(constantQ.DurationInSeconds > 0);
        Assert.NotEmpty(constantQ.ChannelCQTFrames);
        Assert.Contains(constantQ.ChannelCQTFrames.Values.SelectMany(static frames => frames),
            static frame => frame.Spectrum.Length == 24);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MidiFileCustomSerializerDeserializes(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var midi = LoadExport<UMidiFile>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Midi/MIDI_Fixture.uasset", "MIDI_Fixture");
        Assert.Equal(960, midi.TheMidiData.TicksPerQuarterNote);
        Assert.NotEmpty(midi.TheMidiData.Tracks);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PaperSpriteCustomSerializerDeserializes(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var sprite = LoadExport<UPaperSprite>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Paper2D/Sprite_Fixture.uasset", "Sprite_Fixture");
        Assert.Equal((16f, 16f), (sprite.BakedSourceDimension.X, sprite.BakedSourceDimension.Y));
        Assert.False(sprite.BakedSourceTexture?.IsNull ?? true);
        Assert.NotEmpty(sprite.BakedRenderData);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void ComputeGraphCustomSerializerStaysAligned(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var computeGraph = LoadExport<UComputeGraph>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Compute/CG_Fixture.uasset", "CG_Fixture");
        Assert.Empty(computeGraph.KernelResources);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void GeometryCacheCustomSerializerDeserializesStreamedChunk(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var geometryExports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/Geometry/GeoCache_Fixture.uasset");
        var streamable = Assert.Single(geometryExports.OfType<UGeometryCacheTrackStreamable>());
        Assert.False(streamable.Codec.IsNull);
        var chunk = Assert.Single(streamable.Chunks);
        Assert.True(chunk.DataSize > 0);
        var chunkData = Assert.IsType<byte[]>(chunk.BulkData.Data);
        Assert.True(chunkData.Length >= chunk.DataSize);
        Assert.Equal((0f, 0f), (chunk.FirstFrame, chunk.LastFrame));
        var sample = Assert.Single(streamable.Samples);
        Assert.Equal((768, 768), (sample.NumVertices, sample.NumIndices));
        Assert.True(Assert.Single(streamable.VisibilitySamples).bVisibilityState);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PcgGraphDeserializesInputAndOutputNodes(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var pcg = LoadExport<UObject>(provider,
            "CUE4ParseFixtures/Content/Fixtures/PCG/PCG_Fixture.uasset", "PCG_Fixture");
        Assert.Equal("PCGGraph", pcg.ExportType);
        Assert.NotNull(pcg.Get<FPackageIndex>("InputNode").Load<UObject>());
        Assert.NotNull(pcg.Get<FPackageIndex>("OutputNode").Load<UObject>());
    }
}
