using CUE4Parse.MappingsProvider;
using CUE4Parse.MappingsProvider.Usmap;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureMappingsTests
{
    [Fact]
    public void CompressedAndUncompressedMappingsAreEquivalent()
    {
        var compressed = Parse("CUE4ParseFixtures-Oodle.usmap");
        var uncompressed = Parse("CUE4ParseFixtures-Uncompressed.usmap");

        Assert.Equal(EUsmapCompressionMethod.Oodle, compressed.CompressionMethod);
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
        AssertSchema(
            mappings,
            "ParserFixtureBaseData",
            "DataAsset",
            "BaseMarker", "BaseString");
        AssertSchema(
            mappings,
            "ParserFixtureData",
            "ParserFixtureBaseData",
            "bBoolean", "Byte", "Integer", "Integer64", "Float", "Double", "String", "Name", "Text",
            "Vector", "Rotator", "Quat", "Transform", "Guid", "Enum", "Nested", "IntegerArray", "StructArray",
            "NameSet", "IntegerMap", "StructMap", "HardObjectReference", "SoftObjectReference", "ClassReference",
            "SoftClassReference", "HardTextureReference", "SoftTextureReference");

        var fixtureData = mappings.Types["ParserFixtureData"];
        AssertProperty(fixtureData, "Integer", "IntProperty");
        AssertProperty(fixtureData, "IntegerArray", "ArrayProperty", innerType: "IntProperty");
        AssertProperty(fixtureData, "Nested", "StructProperty", structType: "FixtureNestedStruct");
        AssertProperty(fixtureData, "NameSet", "SetProperty", innerType: "NameProperty");
        AssertProperty(fixtureData, "IntegerMap", "MapProperty", innerType: "NameProperty", valueType: "IntProperty");
        AssertProperty(fixtureData, "StructMap", "MapProperty", innerType: "NameProperty", valueType: "StructProperty");
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
        Assert.Equal(properties.Length, schema.PropertyCount);
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
