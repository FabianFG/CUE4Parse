using CUE4Parse.MappingsProvider;
using CUE4Parse.MappingsProvider.Usmap;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureMappingsTests
{
    [Theory]
    [InlineData("CUE4ParseFixtures-Oodle.usmap", EUsmapCompressionMethod.Oodle)]
    [InlineData("CUE4ParseFixtures-Brotli.usmap", EUsmapCompressionMethod.Brotli)]
    [InlineData("CUE4ParseFixtures-Zstandard.usmap", EUsmapCompressionMethod.ZStandard)]
    public void CompressedAndUncompressedMappingsAreEquivalent(
        string compressedFileName,
        EUsmapCompressionMethod expectedCompression)
    {
        var compressed = Parse(compressedFileName);
        var uncompressed = Parse("CUE4ParseFixtures-Uncompressed.usmap");

        Assert.Equal(expectedCompression, compressed.CompressionMethod);
        Assert.Equal(EUsmapCompressionMethod.None, uncompressed.CompressionMethod);

        var compressedMappings = Assert.IsType<TypeMappings>(compressed.Mappings);
        var uncompressedMappings = Assert.IsType<TypeMappings>(uncompressed.Mappings);

        Assert.Equal(
            uncompressedMappings.Types.Keys.Order(StringComparer.OrdinalIgnoreCase),
            compressedMappings.Types.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            uncompressedMappings.Enums.Keys.Order(StringComparer.Ordinal),
            compressedMappings.Enums.Keys.Order(StringComparer.Ordinal));

        foreach (var name in uncompressedMappings.Types.Keys)
        {
            AssertStructsEqual(uncompressedMappings.Types[name], compressedMappings.Types[name]);
        }

        foreach (var name in uncompressedMappings.Enums.Keys)
        {
            Assert.Equal(uncompressedMappings.Enums[name], compressedMappings.Enums[name]);
        }
    }

    [Fact]
    public void MappingContainsNativeFixtureSchemas()
    {
        var parser = Parse("CUE4ParseFixtures-Uncompressed.usmap");
        var mappings = Assert.IsType<TypeMappings>(parser.Mappings);

        AssertSchema(
            mappings,
            "FixtureNestedStruct",
            null,
            "Marker", "Label", "Position", "Samples", "bEnabled");
        AssertSchema(
            mappings,
            "FixtureTableRow",
            "TableRowBase",
            "Number", "LargeNumber", "Message", "Kind", "Nested", "Values");
        AssertSchema(mappings, "FixtureInstancedBase", null, "BaseMarker");
        AssertSchema(
            mappings,
            "FixtureInstancedScalar",
            "FixtureInstancedBase",
            "Value", "Label");
        AssertSchema(
            mappings,
            "FixtureInstancedComposite",
            "FixtureInstancedBase",
            "Nested", "Values", "Text");
        AssertSchema(
            mappings,
            "FixtureInlineObject",
            "Object",
            "Marker", "Label", "Nested");
        AssertSchema(
            mappings,
            "ParserFixtureBaseData",
            "DataAsset",
            "BaseMarker", "BaseString");
        AssertSchema(
            mappings,
            "ParserFixtureData",
            "ParserFixtureBaseData",
            "bBoolean", "Byte", "Integer", "Integer64", "Integer8", "Integer16", "UnsignedInteger16",
            "UnsignedInteger32", "UnsignedInteger64", "Float", "Double", "String", "AnsiString", "Utf8String",
            "Name", "RegistryMarker", "Text", "Vector",
            "Vector2D", "Vector4", "IntPoint", "IntVector", "Color", "LinearColor", "Box", "DateTime",
            "Timespan", "Rotator", "Quat", "Transform", "Guid", "FixedIntegerArray", "PresentOptional",
            "EmptyOptional", "Enum", "Nested", "IntegerArray", "StructArray", "NameSet", "IntegerMap",
            "StructMap", "EmptyInstancedStruct", "ScalarInstancedStruct", "CompositeInstancedStruct",
            "InstancedStructArray", "InstancedStructMap", "TypedInstancedStruct", "HardObjectReference",
            "SoftObjectReference", "ClassReference", "SoftClassReference", "HardTextureReference",
            "SoftTextureReference", "WeakObjectReference", "LazyObjectReference", "InterfaceReference",
            "FieldPathReference", "InlineObject", "DynamicDelegate",
            "DynamicMulticastDelegate", "TextHistories");

        var fixtureData = mappings.Types["ParserFixtureData"];
        // FixedIntegerArray occupies three consecutive schema indices.
        Assert.Equal(63, fixtureData.PropertyCount);
        AssertProperty(fixtureData, "Integer", "IntProperty");
        AssertProperty(fixtureData, "AnsiString", "AnsiStrProperty");
        AssertProperty(fixtureData, "Utf8String", "Utf8StrProperty");
        AssertProperty(fixtureData, "LazyObjectReference", "LazyObjectProperty");
        AssertProperty(fixtureData, "InterfaceReference", "InterfaceProperty");
        AssertProperty(fixtureData, "FieldPathReference", "FieldPathProperty");
        AssertProperty(fixtureData, "PresentOptional", "OptionalProperty", innerType: "IntProperty");
        AssertProperty(fixtureData, "IntegerArray", "ArrayProperty", innerType: "IntProperty");
        AssertProperty(fixtureData, "Nested", "StructProperty", structType: "FixtureNestedStruct");
        AssertProperty(fixtureData, "NameSet", "SetProperty", innerType: "NameProperty");
        AssertProperty(fixtureData, "IntegerMap", "MapProperty", innerType: "NameProperty", valueType: "IntProperty");
        AssertProperty(fixtureData, "StructMap", "MapProperty", innerType: "NameProperty", valueType: "StructProperty");
        AssertProperty(fixtureData, "ScalarInstancedStruct", "StructProperty", structType: "InstancedStruct");
        AssertProperty(fixtureData, "InstancedStructArray", "ArrayProperty", innerType: "StructProperty");
        AssertProperty(fixtureData, "InstancedStructMap", "MapProperty", innerType: "NameProperty", valueType: "StructProperty");
        AssertProperty(fixtureData, "TypedInstancedStruct", "StructProperty", structType: "InstancedStruct");
    }

    private static UsmapParser Parse(string fileName)
    {
        var path = FixturePath("Mappings", fileName);
        Assert.True(File.Exists(path), $"Missing test fixture: {path}");
        return new UsmapParser(path, fileName);
    }

    private static void AssertSchema(
        TypeMappings mappings,
        string name,
        string? superType,
        params ReadOnlySpan<string> properties)
    {
        Assert.True(mappings.Types.TryGetValue(name, out var schema), $"Mapping does not contain {name}");
        Assert.Equal(superType, schema.SuperType);
        // Shipping FNames are case-insensitive when WITH_CASE_PRESERVING_NAME=0.
        // For example, the native member `Float` is emitted as `float` because
        // the primitive type name entered the name pool first.
        var expectedProperties = new HashSet<string>(properties.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties)
            expectedProperties.Add(property);
        var actualProperties = schema.Properties.Values.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = expectedProperties.Except(actualProperties, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var unexpected = actualProperties.Except(expectedProperties, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.True(
            missing.Length == 0 && unexpected.Length == 0,
            $"{name} properties differ. Missing: [{string.Join(", ", missing)}]. " +
            $"Unexpected: [{string.Join(", ", unexpected)}]. Actual: [{string.Join(", ", actualProperties.Order())}]");
    }

    private static void AssertProperty(
        Struct schema,
        string name,
        string type,
        string? structType = null,
        string? innerType = null,
        string? valueType = null)
    {
        var property = Assert.Single(schema.Properties.Values, x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(type, property.MappingType.Type);
        Assert.Equal(structType, property.MappingType.StructType);
        Assert.Equal(innerType, property.MappingType.InnerType?.Type);
        Assert.Equal(valueType, property.MappingType.ValueType?.Type);
    }

    private static void AssertStructsEqual(Struct expected, Struct actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SuperType, actual.SuperType);
        Assert.Equal(expected.PropertyCount, actual.PropertyCount);
        Assert.Equal(expected.Properties.Keys.Order(), actual.Properties.Keys.Order());

        foreach (var index in expected.Properties.Keys)
        {
            var expectedProperty = expected.Properties[index];
            var actualProperty = actual.Properties[index];
            Assert.Equal(expectedProperty.Index, actualProperty.Index);
            Assert.Equal(expectedProperty.Name, actualProperty.Name);
            Assert.Equal(expectedProperty.ArraySize, actualProperty.ArraySize);
            AssertPropertyTypesEqual(expectedProperty.MappingType, actualProperty.MappingType);
        }
    }

    private static void AssertPropertyTypesEqual(PropertyType expected, PropertyType actual)
    {
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.StructType, actual.StructType);
        Assert.Equal(expected.EnumName, actual.EnumName);
        Assert.Equal(expected.IsEnumAsByte, actual.IsEnumAsByte);

        if (expected.InnerType is null)
        {
            Assert.Null(actual.InnerType);
        }
        else
        {
            Assert.NotNull(actual.InnerType);
            AssertPropertyTypesEqual(expected.InnerType, actual.InnerType);
        }

        if (expected.ValueType is null)
        {
            Assert.Null(actual.ValueType);
        }
        else
        {
            Assert.NotNull(actual.ValueType);
            AssertPropertyTypesEqual(expected.ValueType, actual.ValueType);
        }
    }
}
