using System.Buffers.Binary;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.ControlRig;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.NNE;
using CUE4Parse.UE4.Assets.Exports.PCG;
using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Objects.Engine.VectorField;
using CUE4Parse.UE4.Objects.PCG;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureAdvancedAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void AdditiveAnimationPreservesBasePoseAndCompressedTracks(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var animation = LoadExport<UAnimSequence>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Animations/AS_Additive.uasset", "AS_Additive");
        Assert.Equal(EAdditiveAnimationType.AAT_LocalSpaceBase, animation.AdditiveAnimType);
        Assert.Equal(EAdditiveBasePoseType.ABPT_AnimFrame, animation.RefPoseType);
        Assert.Equal("AS_Fixture", animation.RefPoseSeq?.Name.Text);
        Assert.Equal(0.875f, animation.RateScale);
        Assert.NotEmpty(animation.CompressedTrackToSkeletonMapTable);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void BlueprintConstructionScriptAndInheritedOverrideDeserialize(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var parentExports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/Blueprints/BP_Components.uasset");
        var constructionScript = Assert.Single(parentExports.OfType<USimpleConstructionScript>());
        var root = Assert.Single(constructionScript.GetRootNodes(), node => node.InternalVariableName == "FixtureRoot");
        var meshNode = Assert.Single(root.GetChildNodes(), node => node.InternalVariableName == "FixtureMesh");
        var rootTemplate = Assert.IsType<USceneComponent>(root.GetComponentTemplate());
        Assert.Equal((11d, -22d, 33d),
            (rootTemplate.RelativeLocation.X, rootTemplate.RelativeLocation.Y, rootTemplate.RelativeLocation.Z));
        var meshTemplate = Assert.IsType<UStaticMeshComponent>(meshNode.GetComponentTemplate());
        Assert.Equal("SM_Fixture", meshTemplate.GetLoadedStaticMesh()?.Name);
        Assert.Equal((100d, 200d, 300d),
            (meshTemplate.RelativeLocation.X, meshTemplate.RelativeLocation.Y, meshTemplate.RelativeLocation.Z));

        var childExports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/Blueprints/BP_ComponentsChild.uasset");
        var handler = Assert.Single(childExports.OfType<UInheritableComponentHandler>());
        var record = Assert.Single(handler.Records);
        Assert.Equal("FixtureMesh", record.ComponentKey.SCSVariableName.Text);
        var overrideTemplate = Assert.IsType<UStaticMeshComponent>(record.ComponentTemplate?.Load());
        Assert.Equal((-400d, 500d, 600d),
            (overrideTemplate.RelativeLocation.X, overrideTemplate.RelativeLocation.Y,
                overrideTemplate.RelativeLocation.Z));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void FoliageActorDeserializesTypeAndCookedInstances(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/Maps/Instancing.umap");
        var foliage = Assert.Single(exports.OfType<AInstancedFoliageActor>());
        var info = Assert.Single(foliage.FoliageInfos!);
        Assert.Equal("FT_Fixture", info.Key.Name);
        Assert.Equal(EFoliageImplType.StaticMesh, info.Value.Type);
        var implementation = Assert.IsType<FFoliageStaticMesh>(info.Value.Implementation);
        var component = Assert.IsType<UFoliageInstancedStaticMeshComponent>(implementation.Component.Load());
        Assert.Equal(5, component.GetInstances().Length);
        Assert.Equal("SM_Fixture", component.GetLoadedStaticMesh()?.Name);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void LevelInstancePreservesCookedWorldReference(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/Maps/Streaming.umap");
        var levelInstance = Assert.Single(exports.OfType<ALevelInstance>());
        Assert.True(levelInstance.LevelInstanceActorGuid.IsValid());
        Assert.Equal("/Game/Fixtures/Maps/Streaming_Sublevel.Streaming_Sublevel",
            levelInstance.Get<FSoftObjectPath>("CookedWorldAsset").ToString());
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void PcgParamDataDeserializesMetadataDomainHeader(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/PCG/PCG_Parameters.uasset");
        var metadata = Assert.Single(exports.OfType<UPCGMetadata>());
        Assert.Equal(EPCGMetadataDomainFlag.Elements, metadata.ArchiveDefaultDomain?.Flag);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void NneModelDataDeserializesCookedRuntimePayload(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var model = LoadExport<UNNEModelData>(provider,
            "CUE4ParseFixtures/Content/Fixtures/NNE/NNE_Identity.uasset", "NNE_Identity");
        Assert.Empty(model.TargetRuntimes);
        Assert.Equal(string.Empty, model.FileType);
        Assert.Empty(model.FileData);
        Assert.True(model.ModelData.TryGetValue("NNERuntimeORTCpu", out var runtimeData));
        Assert.NotEmpty(runtimeData);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void StaticVectorFieldDeserializesDimensionsAndCookedPayload(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var vectorField = LoadExport<UVectorFieldStatic>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Effects/VF_Static.uasset", "VF_Static");
        Assert.Equal((4, 3, 2),
            (vectorField.Get<int>("SizeX"), vectorField.Get<int>("SizeY"), vectorField.Get<int>("SizeZ")));
        Assert.Equal(4 * 3 * 2 * 8, vectorField.SourceData.GetDataSize());
        var data = Assert.IsType<byte[]>(vectorField.SourceData.ReadDataOnce());
        Assert.Equal((0f, 0f, -2f, 0f), ReadHalfColor(data, 0));
        Assert.Equal((1f, -0.5f, 1f, 0f), ReadHalfColor(data, 23));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void ControlRigContainsCookedHierarchy(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(provider,
            "CUE4ParseFixtures/Content/Fixtures/ControlRig/CR_Fixture.uasset");
        var controlRig = Assert.Single(exports, export => export.Name == "CR_Fixture");
        Assert.Contains("ControlRig", controlRig.ExportType, StringComparison.Ordinal);
        var hierarchy = Assert.Single(exports.OfType<URigHierarchy>());
        var bones = hierarchy.Elements.Select(Assert.IsType<FRigBoneElement>).ToArray();
        Assert.Equal(["Root", "Joint_1", "Joint_2"],
            bones.Select(static bone => bone.LoadedKey.Name.Text));
        Assert.True(bones[0].ParentKey.Name.IsNone);
        Assert.Equal("Root", bones[1].ParentKey.Name.Text);
        Assert.Equal("Joint_1", bones[2].ParentKey.Name.Text);
    }

    private static (float R, float G, float B, float A) ReadHalfColor(byte[] data, int index)
    {
        var color = data.AsSpan(index * 8, 8);
        return (
            (float) BinaryPrimitives.ReadHalfLittleEndian(color),
            (float) BinaryPrimitives.ReadHalfLittleEndian(color[2..]),
            (float) BinaryPrimitives.ReadHalfLittleEndian(color[4..]),
            (float) BinaryPrimitives.ReadHalfLittleEndian(color[6..]));
    }
}
