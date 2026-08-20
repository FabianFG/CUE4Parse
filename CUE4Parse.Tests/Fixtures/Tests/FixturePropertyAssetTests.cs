using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixturePropertyAssetTests
{
    private const string FixtureSuffix =
        "CUE4ParseFixtures/Content/Fixtures/Properties/DA_AllProperties.uasset";

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void AllPropertiesDataAssetDeserializes(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var fixture = LoadExport<UObject>(provider, FixtureSuffix, "DA_AllProperties");

        AssertRuntimeValues(fixture);
        AssertExpandedPrimitiveAndStructProperties(fixture);
        AssertInstancedStructVariants(fixture);
        AssertReferencesResolve(fixture, provider);
        AssertInlineObjectDelegatesAndTextHistories(fixture);
    }

    private static void AssertExpandedPrimitiveAndStructProperties(UObject fixture)
    {
        Assert.Equal(-101, fixture.Get<sbyte>("Integer8"));
        Assert.Equal(-12345, fixture.Get<short>("Integer16"));
        Assert.Equal(54321, fixture.Get<ushort>("UnsignedInteger16"));
        Assert.Equal(0xFEDCBA98u, fixture.Get<uint>("UnsignedInteger32"));
        Assert.Equal(0xFEDCBA9876543210ul, fixture.Get<ulong>("UnsignedInteger64"));

        var vector2D = fixture.Get<FVector2D>("Vector2D");
        Assert.Equal(-12.5f, vector2D.X);
        Assert.Equal(34.75f, vector2D.Y);

        var vector4 = fixture.Get<FVector4>("Vector4");
        Assert.Equal(1.25f, vector4.X);
        Assert.Equal(-2.5f, vector4.Y);
        Assert.Equal(3.75f, vector4.Z);
        Assert.Equal(-4.125f, vector4.W);

        var intPoint = fixture.Get<FIntPoint>("IntPoint");
        Assert.Equal(-12345, intPoint.X);
        Assert.Equal(67890, intPoint.Y);
        var intVector = fixture.Get<FIntVector>("IntVector");
        Assert.Equal((-111, 222, -333), (intVector.X, intVector.Y, intVector.Z));

        var color = fixture.Get<FColor>("Color");
        Assert.Equal(((byte) 0x12, (byte) 0x34, (byte) 0x56, (byte) 0x78),
            (color.R, color.G, color.B, color.A));
        var linearColor = fixture.Get<FLinearColor>("LinearColor");
        Assert.Equal((0.125f, 1.5f, -0.25f, 0.75f),
            (linearColor.R, linearColor.G, linearColor.B, linearColor.A));

        var box = fixture.Get<FBox>("Box");
        Assert.Equal((byte) 1, box.IsValid);
        AssertVector(box.Min, -10.5f, -20.25f, -30.125f);
        AssertVector(box.Max, 40.75f, 50.5f, 60.25f);

        var dateTime = fixture.Get<FDateTime>("DateTime");
        Assert.Equal(new DateTime(2024, 2, 29, 12, 34, 56, 789).Ticks, dateTime.Ticks);
        Assert.NotEqual(0, fixture.Get<FDateTime>("Timespan").Ticks);

        Assert.True(fixture.TryGetAllValues<int>(out var fixedIntegers, "FixedIntegerArray"));
        Assert.Equal([int.MinValue, 0, int.MaxValue], fixedIntegers);

        var present = Assert.IsType<OptionalProperty>(GetPropertyTag(fixture, "PresentOptional").Tag);
        Assert.Equal(0x13579BDF, Assert.IsType<int>(present.Value?.GenericValue));
        Assert.DoesNotContain(fixture.Properties,
            property => property.Name.Text.Equals("EmptyOptional", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertInstancedStructVariants(UObject fixture)
    {
        Assert.DoesNotContain(fixture.Properties,
            property => property.Name.Text.Equals("EmptyInstancedStruct", StringComparison.OrdinalIgnoreCase));
        AssertScalar(GetInstancedStruct(fixture, "ScalarInstancedStruct"), 1001, -7654321, "InstancedScalar");
        AssertComposite(GetInstancedStruct(fixture, "CompositeInstancedStruct"));

        var array = fixture.Get<FInstancedStruct[]>("InstancedStructArray");
        Assert.Equal(3, array.Length);
        AssertScalar(array[0], 1001, -7654321, "InstancedScalar");
        AssertComposite(array[1]);
        Assert.Null(array[2].ScriptStruct);

        var map = fixture.Get<Dictionary<FName, FInstancedStruct>>("InstancedStructMap")
            .ToDictionary(pair => pair.Key.Text, pair => pair.Value, StringComparer.Ordinal);
        AssertScalar(map["Scalar"], 1001, -7654321, "InstancedScalar");
        AssertComposite(map["Composite"]);
        Assert.Null(map["Empty"].ScriptStruct);

        AssertScalar(GetInstancedStruct(fixture, "TypedInstancedStruct"), 3003, 24680, "TypedInstancedScalar");
    }

    private static void AssertInlineObjectDelegatesAndTextHistories(UObject fixture)
    {
        var inlineReference = fixture.Get<FPackageIndex>("InlineObject");
        var inlineObject = Assert.IsAssignableFrom<UObject>(inlineReference.Load<UObject>());
        Assert.Equal("InlineFixture", inlineObject.Name);
        Assert.Equal(0x2468ACE, inlineObject.Get<int>("Marker"));
        Assert.Equal("Inline object payload", inlineObject.Get<string>("Label"));
        AssertNested(inlineObject.Get<FStructFallback>("Nested"), 5150, "InlineNested", -5, 1.5f, 0.25f, true);

        var weakReference = fixture.Get<FPackageIndex>("WeakObjectReference");
        Assert.Equal("DT_AllProperties", weakReference.Name);

        var dynamicDelegate = fixture.Get<FScriptDelegate>("DynamicDelegate");
        Assert.Equal("HandleFixtureDelegate", dynamicDelegate.FunctionName.Text);
        Assert.Equal("DA_AllProperties", dynamicDelegate.Object.Name);

        var multicast = fixture.Get<FMulticastScriptDelegate>("DynamicMulticastDelegate");
        var invocation = Assert.Single(multicast.InvocationList);
        Assert.Equal("HandleFixtureDelegate", invocation.FunctionName.Text);
        Assert.Equal("DA_AllProperties", invocation.Object.Name);

        var texts = fixture.Get<FText[]>("TextHistories");
        Assert.Equal(12, texts.Length);
        Assert.Equal([
            ETextHistoryType.None,
            ETextHistoryType.NamedFormat,
            ETextHistoryType.OrderedFormat,
            ETextHistoryType.ArgumentFormat,
            ETextHistoryType.AsNumber,
            ETextHistoryType.AsPercent,
            ETextHistoryType.AsCurrency,
            ETextHistoryType.AsDate,
            ETextHistoryType.AsTime,
            ETextHistoryType.AsDateTime,
            ETextHistoryType.Transform,
            ETextHistoryType.StringTableEntry
        ], texts.Select(text => text.HistoryType).ToArray());

        var none = Assert.IsType<FTextHistory.None>(texts[0].TextHistory);
        Assert.Equal(string.Empty, none.Text);
        var named = Assert.IsType<FTextHistory.NamedFormat>(texts[1].TextHistory);
        Assert.Equal("Named {Count}", named.SourceFmt.Text);
        Assert.Equal(7L, named.Arguments["Count"].Value);
        var ordered = Assert.IsType<FTextHistory.OrderedFormat>(texts[2].TextHistory);
        Assert.Equal("{0} {1}", ordered.SourceFmt.Text);
        Assert.Equal(42L, ordered.Arguments[0].Value);
        Assert.Equal("ordered", Assert.IsType<FText>(ordered.Arguments[1].Value).Text);
        var argument = Assert.IsType<FTextHistory.ArgumentFormat>(texts[3].TextHistory);
        Assert.Equal("{Label}: {Count}", argument.SourceFmt.Text);
        Assert.Equal(["Count", "Label"], argument.Arguments.Select(static value => value.ArgumentName));
        Assert.Equal(31415L, argument.Arguments[0].ArgumentValue.Value);
        Assert.Equal("argument data", Assert.IsType<FText>(argument.Arguments[1].ArgumentValue.Value).Text);
        var currency = Assert.IsType<FTextHistory.FormatNumber>(texts[6].TextHistory);
        Assert.Equal("EUR", currency.CurrencyCode);
        var dateTime = Assert.IsType<FTextHistory.AsDateTime>(texts[9].TextHistory);
        Assert.Equal(EDateTimeStyle.Full, dateTime.DateStyle);
        Assert.Equal(EDateTimeStyle.Long, dateTime.TimeStyle);
        var transform = Assert.IsType<FTextHistory.Transform>(texts[10].TextHistory);
        Assert.Equal(ETransformType.ToUpper, transform.TransformType);
        Assert.Equal("Mixed Case", transform.SourceText.Text);
        Assert.Equal("Hello from Frankfurt", texts[11].Text);
    }

    private static void AssertRuntimeValues(UObject fixture)
    {
        Assert.Equal(0x0BADF00D, fixture.Get<int>("BaseMarker"));
        Assert.Equal("Inherited_Base_Value", fixture.Get<string>("BaseString"));
        Assert.True(fixture.Get<bool>("bBoolean"));
        Assert.Equal(0xAB, fixture.Get<byte>("Byte"));
        Assert.Equal(0x12345678, fixture.Get<int>("Integer"));
        Assert.Equal(0x1122334455667788, fixture.Get<long>("Integer64"));
        Assert.Equal(1234.25f, GetProperty<float>(fixture, "Float"));
        Assert.Equal(-987654.125, fixture.Get<double>("Double"));
        Assert.Equal("CUE4Parse_日本語_äöü", fixture.Get<string>("String"));
        Assert.Equal("Fixture_ANSI_123", fixture.Get<string>("AnsiString"));
        Assert.Equal("Fixture_UTF8_日本語_Ω", fixture.Get<string>("Utf8String"));
        Assert.Equal("Fixture_Name_0xC0FFEE", fixture.Get<FName>("Name").Text);
        Assert.Equal("Registry_0xC0FFEE", fixture.Get<FName>("RegistryMarker").Text);
        Assert.Equal("Localized-ish fixture text Ω", fixture.Get<FText>("Text").Text);

        AssertVector(fixture.Get<FVector>("Vector"), 1.25f, -2.5f, 3.75f);
        var rotator = fixture.Get<FRotator>("Rotator");
        Assert.Equal((10.0f, 20.0f, 30.0f), (rotator.Pitch, rotator.Yaw, rotator.Roll));

        var quat = fixture.Get<FQuat>("Quat");
        Assert.Equal(0.1825741858f, quat.X, 6);
        Assert.Equal(-0.3651483717f, quat.Y, 6);
        Assert.Equal(0.5477225575f, quat.Z, 6);
        Assert.Equal(0.7302967433f, quat.W, 6);

        var transform = fixture.Get<FTransform>("Transform");
        AssertVector(transform.Translation, 100.5f, -200.25f, 300.125f);
        AssertVector(transform.Scale3D, 1.5f, 2.0f, 0.5f);
        Assert.Equal(quat.X, transform.Rotation.X, 6);
        Assert.Equal(quat.Y, transform.Rotation.Y, 6);
        Assert.Equal(quat.Z, transform.Rotation.Z, 6);
        Assert.Equal(quat.W, transform.Rotation.W, 6);

        var guid = fixture.Get<FGuid>("Guid");
        Assert.Equal(0xDEADBEEFu, guid.A);
        Assert.Equal(0x01234567u, guid.B);
        Assert.Equal(0x89ABCDEFu, guid.C);
        Assert.Equal(0x13579BDFu, guid.D);
        Assert.EndsWith("Beta", fixture.Get<FName>("Enum").Text, StringComparison.Ordinal);

        AssertNested(fixture.Get<FStructFallback>("Nested"), 777, "PrimaryNested_日本語", 7, 8, 9, true);
        Assert.Equal([11, 22, 33, 44], fixture.Get<int[]>("IntegerArray"));

        var structArray = fixture.Get<FStructFallback[]>("StructArray");
        Assert.Equal(3, structArray.Length);
        AssertNested(structArray[0], 1, "First", 1, 2, 3, true);
        AssertNested(structArray[1], 2, "Second", -4, 5, -6, false);
        AssertNested(structArray[2], 3, "Third", 7.5f, 8.5f, 9.5f, true);

        var nameSet = fixture.Get<FName[]>("NameSet").Select(name => name.Text).ToHashSet(StringComparer.Ordinal);
        Assert.True(nameSet.SetEquals(["Set_Alpha", "Set_Beta", "Set_日本語"]));

        var integerMap = fixture.Get<Dictionary<FName, int>>("IntegerMap")
            .ToDictionary(pair => pair.Key.Text, pair => pair.Value, StringComparer.Ordinal);
        Assert.Equal(int.MinValue, integerMap["Minimum"]);
        Assert.Equal(42, integerMap["Answer"]);
        Assert.Equal(0x12345678, integerMap["Marker"]);

        var structMap = fixture.Get<Dictionary<FName, FStructFallback>>("StructMap")
            .ToDictionary(pair => pair.Key.Text, pair => pair.Value, StringComparer.Ordinal);
        AssertNested(structMap["Left"], 314, "MapLeft", -1, -2, -3, true);
        AssertNested(structMap["Right"], 271, "MapRight", 4, 5, 6, false);

        Assert.Equal("DT_AllProperties", fixture.Get<FPackageIndex>("HardObjectReference").Name);
        Assert.Equal(
            "/Game/Fixtures/DataTables/DT_AllProperties.DT_AllProperties",
            fixture.Get<FSoftObjectPath>("SoftObjectReference").ToString());
        Assert.Equal("T_BC7", fixture.Get<FPackageIndex>("HardTextureReference").Name);
        Assert.Equal(
            "/Game/Fixtures/Textures/T_BC6H.T_BC6H",
            fixture.Get<FSoftObjectPath>("SoftTextureReference").ToString());
        Assert.Equal("Texture2D", fixture.Get<FPackageIndex>("ClassReference").Name);
        Assert.Equal(
            "/Script/Engine.Texture2D",
            fixture.Get<FSoftObjectPath>("SoftClassReference").ToString());

        var lazyReference = fixture.Get<FUniqueObjectGuid>("LazyObjectReference");
        Assert.NotEqual(default, lazyReference.Guid);
        Assert.Equal("DA_AllProperties", fixture.Get<FScriptInterface>("InterfaceReference").Object?.Name);
        var fieldPath = fixture.Get<FFieldPath>("FieldPathReference");
        Assert.Equal("Integer", Assert.Single(fieldPath.Path).Text, ignoreCase: true);
    }

    private static void AssertReferencesResolve(UObject fixture, DefaultFileProvider provider)
    {
        var hardTableReference = fixture.Get<FPackageIndex>("HardObjectReference");
        var hardTable = Assert.IsType<UDataTable>(hardTableReference.Load<UDataTable>());
        Assert.Equal("DT_AllProperties", hardTable.Name);
        Assert.True(hardTable.TryGetDataTableRow("Alpha", StringComparison.Ordinal, out var hardAlpha));
        Assert.Equal(0x12345678, hardAlpha.Get<int>("Number"));

        var hardTextureReference = fixture.Get<FPackageIndex>("HardTextureReference");
        var hardTexture = Assert.IsType<UTexture2D>(hardTextureReference.Load<UTexture2D>());
        Assert.Equal("T_BC7", hardTexture.Name);
        Assert.Equal(EPixelFormat.PF_BC7, hardTexture.Format);

        var softTablePath = fixture.Get<FSoftObjectPath>("SoftObjectReference");
        var softTable = provider.LoadPackageObject<UDataTable>(softTablePath.ToString());
        Assert.Equal("DT_AllProperties", softTable.Name);
        Assert.True(softTable.TryGetDataTableRow("Beta", StringComparison.Ordinal, out var softBeta));
        Assert.Equal(-202020, softBeta.Get<int>("Number"));

        var softTexturePath = fixture.Get<FSoftObjectPath>("SoftTextureReference");
        var softTexture = provider.LoadPackageObject<UTexture2D>(softTexturePath.ToString());
        Assert.Equal("T_BC6H", softTexture.Name);
        Assert.Equal(EPixelFormat.PF_BC6H, softTexture.Format);
    }

    private static FInstancedStruct GetInstancedStruct(IPropertyHolder holder, string name)
    {
        var property = Assert.IsType<StructProperty>(GetPropertyTag(holder, name).Tag);
        return Assert.IsType<FInstancedStruct>(property.Value?.StructType);
    }

    private static void AssertScalar(FInstancedStruct value, int baseMarker, int scalar, string label)
    {
        var payload = Assert.IsType<FStructFallback>(value.ScriptStruct?.StructType);
        Assert.Equal(baseMarker, payload.Get<int>("BaseMarker"));
        Assert.Equal(scalar, payload.Get<int>("Value"));
        Assert.Equal(label, payload.Get<FName>("Label").Text);
    }

    private static void AssertComposite(FInstancedStruct value)
    {
        var payload = Assert.IsType<FStructFallback>(value.ScriptStruct?.StructType);
        Assert.Equal(2002, payload.Get<int>("BaseMarker"));
        AssertNested(payload.Get<FStructFallback>("Nested"), 909, "InstancedComposite", 9.25f, -8.5f, 7.75f, true);
        Assert.Equal([3, 1, 4, 1, 5, 9], payload.Get<int[]>("Values"));
        Assert.Equal("Composite instanced payload Ω", payload.Get<FText>("Text").Text);
    }
}
