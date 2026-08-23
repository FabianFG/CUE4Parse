using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.MetaSound;
using CUE4Parse.UE4.Assets.Exports.Niagara;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Niagara;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureEffectAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void NiagaraSystemPreservesExposedUserParameters(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var system = LoadExport<UNiagaraSystem>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Effects/NS_Fixture.uasset",
            "NS_Fixture");

        var exposed = system.Get<FStructFallback>("ExposedParameters");
        var offsets = exposed.Get<FNiagaraVariableWithOffset[]>("SortedParameterOffsets");
        var scale = Assert.Single(offsets, parameter => parameter.Name.Text == "User.FixtureScale");
        var data = exposed.Get<byte[]>("ParameterData");
        Assert.InRange(scale.Offset, 0, data.Length - sizeof(float));
        Assert.Equal(3.25f, BitConverter.ToSingle(data, scale.Offset));

        var textureInterface = Assert.Single(exposed.Get<FPackageIndex[]>("DataInterfaces"));
        var dataInterface = Assert.IsType<UNiagaraDataInterfaceTexture>(
            textureInterface.Load<UNiagaraDataInterfaceTexture>());
        Assert.Equal("T_BC3", dataInterface.Get<FPackageIndex>("Texture").Load<UTexture2D>()?.Name);
        Assert.NotNull(system.NiagaraEmitterCompiledDataStructs);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MetaSoundSourcePreservesFrontendDocument(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var source = LoadExport<UMetaSoundSource>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Effects/MS_Source.uasset",
            "MS_Source");

        Assert.Equal(EMetaSoundOutputAudioFormat.Mono, source.OutputFormat);
        AssertMetaSoundDocument(source, "RootMetasoundDocument", source.RootMetasoundDocument);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MetaSoundPatchPreservesFrontendDocument(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var patch = LoadExport<UMetaSoundPatch>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Effects/MS_Patch.uasset",
            "MS_Patch");

        AssertMetaSoundDocument(patch, "RootMetaSoundDocument", patch.RootMetaSoundDocument);
    }

    private static void AssertMetaSoundDocument(
        UObject asset,
        string propertyName,
        FMetasoundFrontendDocument? document)
    {
        if (document?.RootGraph is not null)
        {
            Assert.NotEmpty(document.RootGraph.PagedGraphs);
            return;
        }

        var documentFallback = GetProperty<FStructFallback>(asset, propertyName);
        var rootGraph = documentFallback.Get<FStructFallback>("RootGraph");
        Assert.NotEmpty(rootGraph.Get<FStructFallback[]>("PagedGraphs"));
    }
}
