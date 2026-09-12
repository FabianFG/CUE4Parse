using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.GeometryFramework;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.WorldPartition;
using CUE4Parse.UE4.Assets.Exports.WorldPartition.DataLayer;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Dto;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureWorldAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void LandscapeLayerInfoPreservesRuntimeSettings(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var layer = LoadExport<ULandscapeLayerInfoObject>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Landscape/LI_Fixture.uasset",
            "LI_Fixture");

        Assert.Equal("FixtureLayer", layer.LayerName.Text);
        Assert.True(layer.PhysMaterial is null || layer.PhysMaterial.IsNull);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void LandscapeMapPreservesHeightAndMeshData(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Maps/Landscape.umap");
        var landscape = Assert.Single(exports.OfType<ALandscape>());

        var material = Assert.IsType<UMaterial>(landscape.LandscapeMaterial.Load<UMaterial>());
        Assert.Equal("M_Landscape", material.Name);
        Assert.Equal((63, 63, 1),
            (landscape.ComponentSizeQuads, landscape.SubsectionSizeQuads, landscape.NumSubsections));
        var component = Assert.IsType<ULandscapeComponent>(
            Assert.Single(landscape.LandscapeComponents).Load<ULandscapeComponent>());
        Assert.Equal((0, 0, 63, 63, 1),
            (component.SectionBaseX, component.SectionBaseY, component.ComponentSizeQuads,
                component.SubsectionSizeQuads, component.NumSubsections));

        var heightmap = Assert.IsAssignableFrom<UTexture2D>(component.GetHeightmap());
        Assert.Equal((64, 64), (heightmap.PlatformData.SizeX, heightmap.PlatformData.SizeY));
        using var converted = new LandscapeMeshDto(landscape);
        var lod = Assert.Single(converted.LODs);
        Assert.NotEmpty(lod.Vertices);
        Assert.NotEmpty(lod.Indices);
        Assert.Equal(0, lod.Indices.Length % 3);

        var convertedMaterial = Assert.Single(converted.Materials);
        Assert.Equal("M_Landscape", convertedMaterial.SlotName);
        var section = Assert.Single(lod.Sections);
        Assert.Equal(0, section.MaterialIndex);
        Assert.Equal(0, section.FirstIndex);
        Assert.Equal(lod.Indices.Length / 3, section.NumFaces);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void RuntimeDataLayerPreservesTypeFilterAndColor(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var layer = LoadExport<UDataLayerAsset>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/WorldPartition/DLA_Runtime.uasset",
            "DLA_Runtime");

        Assert.Equal(EDataLayerType.Runtime, layer.DataLayerType);
        Assert.Equal(EDataLayerLoadFilter.None, layer.LoadFilter);
        Assert.Equal(byte.MaxValue, layer.DebugColor.A);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void WorldPartitionMapPreservesRuntimeHashAndCells(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Maps/WorldPartition.umap");

        var partition = Assert.Single(exports.OfType<UWorldPartition>());
        Assert.True(partition.bCooked);
        var runtimeHash = Assert.IsType<UWorldPartitionRuntimeHashSet>(
            partition.RuntimeHash?.Load<UWorldPartitionRuntimeHash>());
        Assert.NotEmpty(runtimeHash.RuntimePartitions);
        Assert.NotEmpty(runtimeHash.RuntimeStreamingData);
        var cells = runtimeHash.RuntimeStreamingData
            .SelectMany(data => data.SpatiallyLoadedCells.Concat(data.NonSpatiallyLoadedCells))
            .ToArray();
        Assert.NotEmpty(cells);
        var cell = Assert.IsType<UWorldPartitionRuntimeLevelStreamingCell>(Assert.Single(cells)
            .Load<UWorldPartitionRuntimeCell>());
        Assert.True(cell.bIsSpatiallyLoaded);
        Assert.NotNull(cell.RuntimeCellData);
        Assert.NotNull(cell.LevelStreaming);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void WorldPartitionRuntimeCellLoadsPartitionedActor(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var runtimeCellPackage = GetWorldPartitionRuntimeCellPackageName();
        var exports = LoadPackageExports(
            provider,
            $"CUE4ParseFixtures/Content/{runtimeCellPackage["/Game/".Length..]}.umap");
        var world = Assert.Single(exports.OfType<UWorld>());
        var level = Assert.IsType<ULevel>(world.PersistentLevel.Load<ULevel>());
        var actor = Assert.IsType<AStaticMeshActor>(Assert.Single(
            level.Actors,
            actor => actor?.Load<AActor>()?.Name == "PartitionedMesh")?.Load<AActor>());
        var component = Assert.IsType<UStaticMeshComponent>(
            actor.Get<FPackageIndex>("StaticMeshComponent")
                .Load<UStaticMeshComponent>());
        Assert.Equal("SM_Fixture", component.GetLoadedStaticMesh()?.Name);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void DynamicMeshPackageDeserializesWithoutExposingTopology(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var mesh = LoadExport<UDynamicMesh>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Geometry/DM_Fixture.uasset",
            "DM_Fixture");

        Assert.Equal("DynamicMesh", mesh.ExportType);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void NavigationMapPreservesRecastPayload(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Maps/Navigation.umap");
        var navMesh = Assert.Single(exports.OfType<ARecastNavMesh>());

        Assert.InRange(navMesh.NavMeshVersion, ENavMeshVersion.NAVMESHVER_MIN_COMPATIBLE, ENavMeshVersion.Latest);
        Assert.IsType<FPImplRecastNavMesh>(navMesh.RecastNavMeshImpl);
    }
}
