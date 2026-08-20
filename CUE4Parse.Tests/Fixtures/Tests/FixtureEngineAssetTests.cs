using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.LevelSequence;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.MovieScene;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureEngineAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void MaterialParameterCollectionPreservesParameters(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var collection = LoadExport<UMaterialParameterCollection>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Materials/MPC_Fixture.uasset",
            "MPC_Fixture");

        Assert.Equal(new FGuid(0xA1000001, 0xB2000002, 0xC3000003, 0xD4000004),
            GetProperty<FGuid>(collection, "StateId"));

        var scalar = Assert.Single(GetProperty<FStructFallback[]>(collection, "ScalarParameters"));
        Assert.Equal("FixtureScalar", GetProperty<FName>(scalar, "ParameterName").Text);
        Assert.Equal(new FGuid(0xA1000005, 0xB2000006, 0xC3000007, 0xD4000008),
            GetProperty<FGuid>(scalar, "Id"));
        Assert.Equal(0.375f, GetProperty<float>(scalar, "DefaultValue"));

        var vector = Assert.Single(GetProperty<FStructFallback[]>(collection, "VectorParameters"));
        Assert.Equal("FixtureColor", GetProperty<FName>(vector, "ParameterName").Text);
        Assert.Equal(new FGuid(0xA1000009, 0xB200000A, 0xC300000B, 0xD400000C),
            GetProperty<FGuid>(vector, "Id"));
        var color = GetProperty<FLinearColor>(vector, "DefaultValue");
        Assert.Equal((0.125f, 0.5f, 0.875f, 1.0f), (color.R, color.G, color.B, color.A));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void LevelSequencePreservesMpcTrackAndParameterChannels(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var sequence = LoadExport<ULevelSequence>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Sequences/LS_Fixture.uasset",
            "LS_Fixture");

        var movieScene = Assert.IsAssignableFrom<UObject>(sequence.MovieScene.Load<UObject>());
        AssertFrameRate(movieScene.Get<FStructFallback>("TickResolution"), 24_000, 1);
        AssertFrameRate(movieScene.Get<FStructFallback>("DisplayRate"), 24, 1);
        var playbackRange = movieScene.Get<FMovieSceneFrameRange>("PlaybackRange").Value;
        Assert.Equal(0, playbackRange.LowerBound.Value.Value);
        Assert.Equal(24_001, playbackRange.UpperBound.Value.Value);

        var trackReference = Assert.Single(movieScene.Get<FPackageIndex[]>("Tracks"));
        var track = Assert.IsAssignableFrom<UObject>(trackReference.Load<UObject>());
        Assert.Equal("MovieSceneMaterialParameterCollectionTrack", track.ExportType);
        Assert.Equal("MPC_Fixture", track.Get<FPackageIndex>("MPC").Load<UMaterialParameterCollection>()?.Name);
        var sectionReference = Assert.Single(track.Get<FPackageIndex[]>("Sections"));
        var section = Assert.IsAssignableFrom<UObject>(sectionReference.Load<UObject>());

        var scalar = Assert.Single(section.Get<FStructFallback[]>("ScalarParameterNamesAndCurves"));
        Assert.Equal("FixtureScalar", GetProperty<FName>(scalar, "ParameterName").Text);
        AssertChannel(GetProperty<FMovieSceneChannel<float>>(scalar, "ParameterCurve"),
            [0, 12_000, 24_000], [0.125f, 0.75f, -0.25f]);

        var color = Assert.Single(section.Get<FStructFallback[]>("ColorParameterNamesAndCurves"));
        Assert.Equal("FixtureColor", GetProperty<FName>(color, "ParameterName").Text);
        AssertChannel(GetProperty<FMovieSceneChannel<float>>(color, "RedCurve"), [0, 24_000], [1.0f, 0.0f]);
        AssertChannel(GetProperty<FMovieSceneChannel<float>>(color, "GreenCurve"), [0, 24_000], [0.0f, 0.75f]);
        AssertChannel(GetProperty<FMovieSceneChannel<float>>(color, "BlueCurve"), [0, 24_000], [0.25f, 1.0f]);
        AssertChannel(GetProperty<FMovieSceneChannel<float>>(color, "AlphaCurve"), [0, 24_000], [1.0f, 0.5f]);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void NestedLevelSequencePreservesSubSequenceTiming(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var sequence = LoadExport<ULevelSequence>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Sequences/LS_Nested.uasset",
            "LS_Nested");

        var movieScene = Assert.IsAssignableFrom<UObject>(sequence.MovieScene.Load<UObject>());
        AssertFrameRate(movieScene.Get<FStructFallback>("TickResolution"), 24_000, 1);
        AssertFrameRate(movieScene.Get<FStructFallback>("DisplayRate"), 24, 1);
        var playbackRange = movieScene.Get<FMovieSceneFrameRange>("PlaybackRange").Value;
        Assert.Equal(0, playbackRange.LowerBound.Value.Value);
        Assert.Equal(48_001, playbackRange.UpperBound.Value.Value);

        var track = Assert.IsAssignableFrom<UObject>(
            Assert.Single(movieScene.Get<FPackageIndex[]>("Tracks")).Load<UObject>());
        Assert.Equal("MovieSceneSubTrack", track.ExportType);
        var sections = track.Get<FPackageIndex[]>("Sections")
            .Select(index => Assert.IsAssignableFrom<UObject>(index.Load<UObject>()))
            .OrderBy(section => section.Get<FMovieSceneFrameRange>("SectionRange").Value.LowerBound.Value.Value)
            .ToArray();
        Assert.Equal(2, sections.Length);

        AssertSubSection(sections[0], 6_000, 30_001, 0.5f, 1_200, 100);
        AssertSubSection(sections[1], 30_001, 42_001, 1.5f, 0, 37);
        Assert.All(sections, section =>
            Assert.Equal("LS_Fixture", section.Get<FPackageIndex>("SubSequence").Load<ULevelSequence>()?.Name));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void BlueprintPreservesGeneratedClassAndDefaultObject(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var exports = LoadPackageExports(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Blueprints/BP_Fixture.uasset");
        var generatedClass = Assert.IsType<UBlueprintGeneratedClass>(
            Assert.Single(exports, export => export.Name == "BP_Fixture_C"));
        var defaultObject = Assert.IsType<UObject>(generatedClass.ClassDefaultObject.Load<UObject>());

        Assert.True(generatedClass.bCooked);
        Assert.Same(Assert.Single(exports, export => export.Name == "Default__BP_Fixture_C"), defaultObject);
        Assert.Equal(-24_680, defaultObject.Get<int>("Integer"));
        Assert.Equal("Cross-package Blueprint fixture", defaultObject.Get<string>("String"));
        Assert.Equal(0x13572468, defaultObject.Get<int>("BaseMarker"));
        Assert.Equal("Blueprint generated class default object", defaultObject.Get<string>("BaseString"));
        Assert.Equal("DA_AllProperties", defaultObject.Get<FPackageIndex>("HardObjectReference").Load<UObject>()?.Name);
        Assert.Equal("/Game/Fixtures/Properties/DA_AllProperties.DA_AllProperties",
            defaultObject.Get<FSoftObjectPath>("SoftObjectReference").ToString());
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void FontPreservesGlyphsTexturePageAndCompositeTypeface(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var font = LoadExport<UFont>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Fonts/Font_Fixture.uasset",
            "Font_Fixture");

        var characters = GetProperty<FFontCharacter[]>(font, "Characters");
        Assert.Equal(128, characters.Length);
        AssertCharacter(characters[32], 0, 0, 4, 12, 0, 1);
        AssertCharacter(characters[65], 4, 0, 8, 12, 0, -2);
        AssertCharacter(characters[66], 12, 0, 7, 12, 0, -1);
        var textureReference = Assert.Single(GetProperty<FPackageIndex[]>(font, "Textures"));
        Assert.Equal("T_BGRA8", Assert.IsType<UTexture2D>(textureReference.Load<UTexture2D>()).Name);

        Assert.Equal(1.0f, GetProperty<float>(font, "EmScale"));
        Assert.Equal(9.5f, GetProperty<float>(font, "Ascent"));
        Assert.Equal(-2.5f, GetProperty<float>(font, "Descent"));
        Assert.Equal(1.25f, GetProperty<float>(font, "Leading"));
        Assert.Equal(1, GetProperty<int>(font, "Kerning"));
        Assert.Equal(12, GetProperty<int>(font, "LegacyFontSize"));
        Assert.Equal("FixtureRegular", GetProperty<FName>(font, "LegacyFontName").Text);

        var composite = GetProperty<FStructFallback>(font, "CompositeFont");
        AssertTypeface(composite.Get<FStructFallback>("DefaultTypeface"), "FixtureRegular");
        var subTypeface = Assert.Single(composite.Get<FStructFallback[]>("SubTypefaces"));
        AssertTypeface(subTypeface.Get<FStructFallback>("Typeface"), "FixtureUnicode");
        Assert.Equal("el;en", subTypeface.Get<string>("Cultures"));
        var characterRange = Assert.Single(subTypeface.Get<FStructFallback[]>("CharacterRanges"));
        var lowerBound = GetProperty<FStructFallback>(characterRange, "LowerBound");
        var upperBound = GetProperty<FStructFallback>(characterRange, "UpperBound");
        Assert.Equal((ERangeBoundTypes.Inclusive, 0x0370),
            (GetProperty<ERangeBoundTypes>(lowerBound, "Type"), GetProperty<int>(lowerBound, "Value")));
        Assert.Equal((ERangeBoundTypes.Inclusive, 0x03FF),
            (GetProperty<ERangeBoundTypes>(upperBound, "Type"), GetProperty<int>(upperBound, "Value")));
    }

    private static void AssertFrameRate(FStructFallback rate, int numerator, int denominator)
    {
        Assert.Equal(numerator, GetProperty<int>(rate, "Numerator"));
        Assert.Equal(denominator, rate.GetOrDefault("Denominator", 1, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertSubSection(
        UObject section,
        int lower,
        int upper,
        float timeScale,
        int startFrameOffset,
        int hierarchicalBias)
    {
        var range = section.Get<FMovieSceneFrameRange>("SectionRange").Value;
        Assert.Equal((lower, upper), (range.LowerBound.Value.Value, range.UpperBound.Value.Value));
        var parameters = section.Get<FStructFallback>("Parameters");
        var timeWarp = parameters.Get<FMovieSceneTimeWarpVariant>("TimeScale");
        Assert.Equal(EMovieSceneTimeWarpType.FixedPlayRate, timeWarp.Type);
        Assert.Equal(timeScale, timeWarp.PlayRate);
        Assert.Equal(startFrameOffset, parameters.GetOrDefault<FFrameNumber>("StartFrameOffset").Value);
        Assert.Equal(hierarchicalBias, parameters.GetOrDefault("HierarchicalBias", 100));
    }

    private static void AssertChannel(FMovieSceneChannel<float> channel, int[] expectedTimes, float[] expectedValues)
    {
        Assert.Equal(expectedTimes, channel.Times.Select(time => time.Value).ToArray());
        Assert.Equal(expectedValues, channel.Values.Select(value => value.Value).ToArray());
        Assert.Equal((60_000, 1), (channel.TickResolution.Numerator, channel.TickResolution.Denominator));
    }

    private static void AssertCharacter(
        FFontCharacter character,
        int startU,
        int startV,
        int width,
        int height,
        byte textureIndex,
        int verticalOffset) =>
        Assert.Equal((startU, startV, width, height, textureIndex, verticalOffset),
            (character.StartU, character.StartV, character.USize, character.VSize,
                character.TextureIndex, character.VerticalOffset));

    private static void AssertTypeface(FStructFallback typeface, string expectedName)
    {
        var entry = Assert.Single(typeface.Get<FStructFallback[]>("Fonts"));
        Assert.Equal(expectedName, entry.Get<FName>("Name").Text);
    }
}
