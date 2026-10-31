using System.Text;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.PCG;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.Tests;

public class PcgMetadataAttributeTests
{
    private const int TypeObjectImport = -1;

    [Fact]
    public void SetAndMapValuesStartWithTheirRemovalCount()
    {
        var set = new PcgStream();
        set.GenericHeader(EPCGMetadataTypes.Integer32, [EPCGMetadataAttributeContainerTypes.Set]);
        set.Int(0).Int(1).Int(7); // default {7}
        set.Int(1).Int(0).Int(2).Int(11).Int(22); // values [{11, 22}]

        var setAttribute = Assert.IsType<FPCGMetadataGenericAttribute>(set.ReadAttribute());
        Assert.Equal([7], Elements<int>(setAttribute.DefaultValue));
        Assert.Equal([11, 22], Elements<int>(Assert.Single(setAttribute.Values)));
        set.AssertFullyRead();

        var map = new PcgStream();
        map.GenericHeader(EPCGMetadataTypes.Float, [EPCGMetadataAttributeContainerTypes.Map], keyType: EPCGMetadataTypes.Integer32);
        map.Int(0).Int(0); // default {}
        map.Int(1).Int(0).Int(1).Int(3).Float(0.5f); // values [{3: 0.5}]

        var mapAttribute = Assert.IsType<FPCGMetadataGenericAttribute>(map.ReadAttribute());
        var entry = Assert.Single(Assert.IsType<MapProperty>(Assert.Single(mapAttribute.Values)).Value!.Properties);
        Assert.Equal(3, entry.Key.GetValue<int>());
        Assert.Equal(0.5f, entry.Value!.GetValue<float>());
        map.AssertFullyRead();
    }

    [Fact]
    public void BoolArraysUseOneBytePerElement()
    {
        var stream = new PcgStream();
        stream.GenericHeader(EPCGMetadataTypes.Boolean, [EPCGMetadataAttributeContainerTypes.Array]);
        stream.Int(0); // default []
        stream.Int(1).Int(3).Byte(1).Byte(0).Byte(1); // values [[true, false, true]]

        var attribute = Assert.IsType<FPCGMetadataGenericAttribute>(stream.ReadAttribute());
        Assert.Equal([true, false, true], Elements<bool>(Assert.Single(attribute.Values)));
        stream.AssertFullyRead();
    }

    [Fact]
    public void EnumValuesAreStoredByName()
    {
        var stream = new PcgStream();
        stream.Imports[TypeObjectImport] = new FixtureObject(stream.Package, "EFixtureEnum");
        stream.GenericHeader(EPCGMetadataTypes.Enum, [], TypeObjectImport);
        stream.Name("EFixtureEnum::B");
        stream.Int(2).Name("EFixtureEnum::A").Name("EFixtureEnum::C");

        var attribute = Assert.IsType<FPCGMetadataGenericAttribute>(stream.ReadAttribute());
        Assert.Equal("EFixtureEnum::B", attribute.DefaultValue!.GetValue<FName>().Text);
        Assert.Equal(["EFixtureEnum::A", "EFixtureEnum::C"], attribute.Values.Select(value => value.GetValue<FName>().Text).ToArray());
        stream.AssertFullyRead();
    }

    [Fact]
    public void UserDefinedStructsAreReadWithTheLoadedStruct()
    {
        var stream = new PcgStream();
        var row = new UScriptStruct
        {
            Name = "FixtureRow",
            ChildProperties =
            [
                new FIntProperty { Name = new FName("Count"), ArrayDim = 1 },
                new FStrProperty { Name = new FName("Label"), ArrayDim = 1 },
            ],
        };
        stream.Imports[TypeObjectImport] = new FixtureObject(stream.Package, "FixtureRow", row);
        stream.GenericHeader(EPCGMetadataTypes.Struct, [], TypeObjectImport);
        stream.Properties(2).Int(1).Str("default");
        stream.Int(1).Properties(2).Int(5).Str("first");

        var attribute = Assert.IsType<FPCGMetadataGenericAttribute>(stream.ReadAttribute());
        var value = Assert.IsType<FStructFallback>(Assert.Single(attribute.Values).GetValue<FScriptStruct>()!.StructType);
        Assert.Equal(5, value.Get<int>("Count"));
        Assert.Equal("first", value.Get<string>("Label"));
        stream.AssertFullyRead();
    }

    [Fact]
    public void StructsThatCantBeResolvedFailInsteadOfSkippingTheirValues()
    {
        var stream = new PcgStream();
        stream.GenericHeader(EPCGMetadataTypes.Struct, [], TypeObjectImport);
        stream.Properties(1).Int(1);
        stream.Int(0);

        Assert.Throws<ParserException>(() => stream.ReadAttribute());
    }

    [Fact]
    public void AttributesUECantBuildAPropertyForHaveNoValues()
    {
        var stream = new PcgStream();
        stream.GenericHeader(EPCGMetadataTypes.Float, [EPCGMetadataAttributeContainerTypes.Map]);

        var attribute = Assert.IsType<FPCGMetadataGenericAttribute>(stream.ReadAttribute());
        Assert.Null(attribute.DefaultValue);
        Assert.Empty(attribute.Values);
        stream.AssertFullyRead();
    }

    [Fact]
    public void GenericTransformsAreReadAsProperties()
    {
        var stream = new PcgStream();
        stream.GenericHeader(EPCGMetadataTypes.Transform, []);
        stream.Transform(new FVector(0, 0, 0), 1);
        stream.Int(1).Transform(new FVector(1, 2, 3), 2);

        var attribute = Assert.IsType<FPCGMetadataAttribute<FTransform>>(stream.ReadAttribute());
        var value = Assert.Single(attribute.Values);
        Assert.Equal((1f, 2f, 3f), (value.Translation.X, value.Translation.Y, value.Translation.Z));
        Assert.Equal(2f, value.Scale3D.X);
        Assert.Equal(1f, value.Rotation.W);
        stream.AssertFullyRead();
    }

    [Fact]
    public void LegacyBoolDefaultsAreStoredAsInt32()
    {
        var stream = new PcgStream(EGame.GAME_UE5_7);
        stream.Int((int) EPCGMetadataTypes.Boolean);
        stream.AttributeHeader();
        stream.Int(2).Byte(1).Byte(0); // values
        stream.Int(1); // default

        var attribute = Assert.IsType<FPCGMetadataAttribute<bool>>(stream.ReadAttribute());
        Assert.Equal([true, false], attribute.Values);
        Assert.True(attribute.DefaultValue);
        stream.AssertFullyRead();
    }

    // Soft paths are left out: both readers use the same FSoftObjectPath constructor for them
    public static TheoryData<EPCGMetadataTypes> LegacyTypes() =>
    [
        EPCGMetadataTypes.Float, EPCGMetadataTypes.Double, EPCGMetadataTypes.Integer32, EPCGMetadataTypes.Integer64,
        EPCGMetadataTypes.Vector2, EPCGMetadataTypes.Vector, EPCGMetadataTypes.Vector4, EPCGMetadataTypes.Quaternion,
        EPCGMetadataTypes.Transform, EPCGMetadataTypes.String, EPCGMetadataTypes.Boolean, EPCGMetadataTypes.Rotator,
        EPCGMetadataTypes.Name,
    ];

    // Single values of the legacy types go through the typed reader, arrays of them through the property reader.
    // Both have to read the same bytes per value, since the two switches describe the same types
    [Theory]
    [MemberData(nameof(LegacyTypes))]
    public void TypedAndPropertyReadersAgreeOnLegacyTypes(EPCGMetadataTypes type)
    {
        var single = new PcgStream();
        single.GenericHeader(type, []);
        single.Value(type); // default
        single.Int(2).Value(type).Value(type); // values

        Assert.IsNotType<FPCGMetadataGenericAttribute>(single.ReadAttribute());
        single.AssertFullyRead();

        var array = new PcgStream();
        array.GenericHeader(type, [EPCGMetadataAttributeContainerTypes.Array]);
        array.Int(1).Value(type); // default
        array.Int(1).Int(2).Value(type).Value(type); // values

        var generic = Assert.IsType<FPCGMetadataGenericAttribute>(array.ReadAttribute());
        Assert.Equal(2, Assert.IsType<ArrayProperty>(Assert.Single(generic.Values)).Value!.Properties.Count);
        array.AssertFullyRead();
    }

    [Fact]
    public void ParentedAttributesUseTheDomainDescriptor()
    {
        var stream = new PcgStream();
        stream.Descriptor(EPCGMetadataTypes.Integer32, [EPCGMetadataAttributeContainerTypes.Array]);
        stream.AttributeHeader(parentAttributeId: 2); // no descriptor of its own
        stream.Int(0); // default []
        stream.Int(1).Int(2).Int(5).Int(6); // values [[5, 6]]

        var attribute = Assert.IsType<FPCGMetadataGenericAttribute>(stream.ReadAttribute());
        Assert.Equal(2, attribute.ParentAttributeId);
        Assert.Equal([5, 6], Elements<int>(Assert.Single(attribute.Values)));
        stream.AssertFullyRead();
    }

    [Fact]
    public void GenericValuesBeforeTheDomainDescriptorStillReadTheAttributeDescriptor()
    {
        // At ConvertFPCGMetadataAttributeToGenericAttributes the domain still writes the type as an int32
        var versions = new FCustomVersionContainer([new FCustomVersion(FFortniteMainBranchObjectVersion.GUID,
            (int) FFortniteMainBranchObjectVersion.Type.ConvertFPCGMetadataAttributeToGenericAttributes)]);
        var stream = new PcgStream(customVersions: versions);
        stream.Int((int) EPCGMetadataTypes.Float);
        stream.AttributeHeader();
        stream.Descriptor(EPCGMetadataTypes.Float, []);
        stream.Float(1); // default
        stream.Int(1).Float(2); // values

        var attribute = Assert.IsType<FPCGMetadataAttribute<float>>(stream.ReadAttribute());
        Assert.Equal(1f, attribute.DefaultValue);
        Assert.Equal([2f], attribute.Values);
        stream.AssertFullyRead();
    }

    [Fact]
    public void RemovedSetAndMapValuesAreSkipped()
    {
        var set = new PcgStream();
        set.GenericHeader(EPCGMetadataTypes.Integer32, [EPCGMetadataAttributeContainerTypes.Set]);
        set.Int(2).Int(98).Int(99).Int(1).Int(7); // default {7}, two removed elements
        set.Int(0); // no values

        Assert.Equal([7], Elements<int>(Assert.IsType<FPCGMetadataGenericAttribute>(set.ReadAttribute()).DefaultValue));
        set.AssertFullyRead();

        var map = new PcgStream();
        map.GenericHeader(EPCGMetadataTypes.Float, [EPCGMetadataAttributeContainerTypes.Map], keyType: EPCGMetadataTypes.Integer32);
        map.Int(1).Int(42).Int(1).Int(3).Float(0.5f); // default {3: 0.5}, removed key 42
        map.Int(0); // no values

        var entry = Assert.Single(Assert.IsType<MapProperty>(Assert.IsType<FPCGMetadataGenericAttribute>(map.ReadAttribute()).DefaultValue).Value!.Properties);
        Assert.Equal((3, 0.5f), (entry.Key.GetValue<int>(), entry.Value!.GetValue<float>()));
        map.AssertFullyRead();
    }

    [Fact]
    public void ByteAndTextValuesAreGeneric()
    {
        var bytes = new PcgStream();
        bytes.GenericHeader(EPCGMetadataTypes.Byte, []);
        bytes.Byte(7);
        bytes.Int(2).Byte(1).Byte(2);

        var byteAttribute = Assert.IsType<FPCGMetadataGenericAttribute>(bytes.ReadAttribute());
        Assert.Equal([1, 2], byteAttribute.Values.Select(value => (int) value.GetValue<byte>()).ToArray());
        bytes.AssertFullyRead();

        var text = new PcgStream();
        text.GenericHeader(EPCGMetadataTypes.Text, []);
        text.Text("default");
        text.Int(1).Text("first");

        var textAttribute = Assert.IsType<FPCGMetadataGenericAttribute>(text.ReadAttribute());
        Assert.Equal("first", Assert.Single(textAttribute.Values).GetValue<FText>()!.Text);
        text.AssertFullyRead();
    }

    [Fact]
    public void DescriptorValuesTheMappingsDontKnowThrow()
    {
        var type = new PcgStream();
        type.GenericHeader((EPCGMetadataTypes) 200, []);
        Assert.Throws<ParserException>(() => type.ReadAttribute());

        var container = new PcgStream();
        container.Descriptor(EPCGMetadataTypes.Float, [EPCGMetadataAttributeContainerTypes.Array], containerName: "EPCGMetadataAttributeContainerTypes::Unknown");
        Assert.Throws<ParserException>(() => container.ReadAttribute());
    }

    [Fact]
    public void LegacyTypeIdsOutOfRangeThrow()
    {
        var stream = new PcgStream(EGame.GAME_UE5_7);
        stream.Int(256);
        stream.AttributeHeader();

        Assert.Throws<ParserException>(() => stream.ReadAttribute());
    }

    [Fact]
    public void RepeatedAttributeNamesKeepTheLastAttribute()
    {
        var stream = new PcgStream(EGame.GAME_UE5_7);
        stream.Int(2);
        foreach (var value in new[] { 1, 2 })
        {
            stream.Name("Attribute");
            stream.Int((int) EPCGMetadataTypes.Integer32);
            stream.AttributeHeader();
            stream.Int(1).Int(value); // values
            stream.Int(0); // default
        }
        stream.Int(0); // ParentKeys

        var attribute = Assert.IsType<FPCGMetadataAttribute<int>>(Assert.Single(stream.ReadDomain().Attributes).Value);
        Assert.Equal([2], attribute.Values);
        stream.AssertFullyRead();
    }

    private static T[] Elements<T>(FPropertyTagType? container) => container switch
    {
        ArrayProperty array => array.Value!.Properties.Select(value => value.GetValue<T>()!).ToArray(),
        SetProperty set => set.Value!.Properties.Select(value => value.GetValue<T>()!).ToArray(),
        _ => throw new ArgumentException($"{container} is not an array or set")
    };

    private sealed class PcgStream
    {
        private readonly MemoryStream _data = new();
        private readonly BinaryWriter _writer;
        private readonly List<string> _names = ["None"];
        private readonly VersionContainer _versions;
        private FAssetArchive? _archive;

        public readonly Dictionary<int, ResolvedObject> Imports = [];
        public readonly FixturePackage Package;

        public PcgStream(EGame game = EGame.GAME_UE5_8, FCustomVersionContainer? customVersions = null)
        {
            _writer = new BinaryWriter(_data);
            _versions = new VersionContainer(game, customVersions: customVersions);
            Package = new FixturePackage(_names, Imports, CreateMappings());
        }

        public PcgStream Int(int value) { _writer.Write(value); return this; }
        public PcgStream Byte(byte value) { _writer.Write(value); return this; }
        public PcgStream Float(float value) { _writer.Write(value); return this; }
        public PcgStream Double(double value) { _writer.Write(value); return this; }

        public PcgStream Name(string name)
        {
            var index = _names.IndexOf(name);
            if (index < 0)
            {
                index = _names.Count;
                _names.Add(name);
            }

            return Int(index).Int(0);
        }

        public PcgStream Str(string value)
        {
            Int(value.Length + 1);
            _writer.Write(Encoding.ASCII.GetBytes(value));
            return Byte(0);
        }

        // Unversioned header for a struct whose first count properties are all serialized
        public PcgStream Properties(int count)
        {
            _writer.Write((ushort) (0x100 | count << 9));
            return this;
        }

        public void Transform(FVector translation, double scale)
        {
            Properties(3);
            Double(0).Double(0).Double(0).Double(1);
            Double(translation.X).Double(translation.Y).Double(translation.Z);
            Double(scale).Double(scale).Double(scale);
        }

        public void AttributeHeader(int parentAttributeId = -1)
        {
            Int(0); // EntryToValueKeyMap
            Int(parentAttributeId);
            Name("Attribute");
            Int(0); // AttributeId
        }

        // The domain writes the descriptor before the attribute, and the attribute writes it again because it has no parent
        public void GenericHeader(EPCGMetadataTypes valueType, EPCGMetadataAttributeContainerTypes[] containers,
            int valueTypeObject = 0, EPCGMetadataTypes keyType = EPCGMetadataTypes.Unknown)
        {
            Descriptor(valueType, containers, valueTypeObject, keyType);
            AttributeHeader();
            Descriptor(valueType, containers, valueTypeObject, keyType);
        }

        public void Descriptor(EPCGMetadataTypes valueType, EPCGMetadataAttributeContainerTypes[] containers,
            int valueTypeObject = 0, EPCGMetadataTypes keyType = EPCGMetadataTypes.Unknown, string? containerName = null)
        {
            Properties(6);
            Name("Attribute");
            Byte((byte) valueType);
            Int(containers.Length);
            foreach (var container in containers)
                Name(containerName ?? $"EPCGMetadataAttributeContainerTypes::{container}");
            Int(valueTypeObject);
            Byte((byte) keyType);
            Int(0);
        }

        public FPCGMetadataAttributeBase ReadAttribute()
        {
            _archive = new FAssetArchive(new FByteArchive("pcg", _data.ToArray(), _versions), Package);
            return FPCGMetadataAttributeBase.ReadPCGMetadataAttribute(_archive, new FName("Attribute"));
        }

        public FPCGMetadataDomain ReadDomain()
        {
            _archive = new FAssetArchive(new FByteArchive("pcg", _data.ToArray(), _versions), Package);
            return new FPCGMetadataDomain(_archive);
        }

        public void AssertFullyRead() => Assert.Equal(_archive!.Length, _archive.Position);

        // A text without history: flags, history type None, then its culture invariant string
        public PcgStream Text(string value)
        {
            Int(0).Byte(0xFF).Int(1);
            return Str(value);
        }

        // One sample value of a legacy type, as both UE layouts write it
        public PcgStream Value(EPCGMetadataTypes type)
        {
            switch (type)
            {
                case EPCGMetadataTypes.Float: return Float(1.5f);
                case EPCGMetadataTypes.Double: return Double(2.5);
                case EPCGMetadataTypes.Integer32: return Int(3);
                case EPCGMetadataTypes.Integer64: return Int(4).Int(0);
                case EPCGMetadataTypes.Vector2: return Double(1).Double(2);
                case EPCGMetadataTypes.Vector or EPCGMetadataTypes.Rotator: return Double(1).Double(2).Double(3);
                case EPCGMetadataTypes.Vector4 or EPCGMetadataTypes.Quaternion: return Double(0).Double(0).Double(0).Double(1);
                case EPCGMetadataTypes.Transform: Transform(new FVector(1, 2, 3), 1); return this;
                case EPCGMetadataTypes.String: return Str("value");
                case EPCGMetadataTypes.Boolean: return Byte(1);
                case EPCGMetadataTypes.Name: return Name("Value");
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static TypeMappings CreateMappings()
        {
            static PropertyType Enum(string name) => new("EnumProperty", innerType: new PropertyType("ByteProperty"), enumName: name);
            static Struct Struct(string name, params (string Name, PropertyType Type)[] properties) =>
                new(null, name, null, properties.Select((property, i) => (i, property))
                    .ToDictionary(pair => pair.i, pair => new PropertyInfo(pair.i, pair.property.Name, pair.property.Type)), properties.Length);
            static Dictionary<long, string> Values<TEnum>() where TEnum : struct, Enum =>
                System.Enum.GetValues<TEnum>().ToDictionary(value => Convert.ToInt64(value), value => value.ToString());

            var desc = Struct("PCGMetadataAttributeDesc",
                ("Name", new PropertyType("NameProperty")),
                ("ValueType", Enum("EPCGMetadataTypes")),
                ("ContainerTypes", new PropertyType("ArrayProperty", innerType: Enum("EPCGMetadataAttributeContainerTypes"))),
                ("ValueTypeObject", new PropertyType("ObjectProperty")),
                ("KeyType", Enum("EPCGMetadataTypes")),
                ("KeyTypeObject", new PropertyType("ObjectProperty")));
            var transform = Struct("Transform",
                ("Rotation", new PropertyType("StructProperty", "Quat")),
                ("Translation", new PropertyType("StructProperty", "Vector")),
                ("Scale3D", new PropertyType("StructProperty", "Vector")));

            return new TypeMappings(
                new Dictionary<string, Struct> { [desc.Name] = desc, [transform.Name] = transform },
                new Dictionary<string, Dictionary<long, string>>
                {
                    ["EPCGMetadataTypes"] = Values<EPCGMetadataTypes>(),
                    ["EPCGMetadataAttributeContainerTypes"] = Values<EPCGMetadataAttributeContainerTypes>(),
                });
        }
    }

    private sealed class FixturePackage(List<string> names, Dictionary<int, ResolvedObject> imports, TypeMappings mappings) : IPackage
    {
        public string Name { get; set; } = "PcgFixture";
        public IFileProvider? Provider => null;
        public string? ArchiveName => null;
        public TypeMappings Mappings => mappings;
        public FPackageFileSummary Summary { get; } = new() { PackageFlags = EPackageFlags.PKG_UnversionedProperties };
        public FNameEntrySerialized[] NameMap => names.Select(name => new FNameEntrySerialized(name)).ToArray();
        public int ImportMapLength => imports.Count;
        public int ExportMapLength => 0;
        public Lazy<UObject>[] ExportsLazy => [];
        public bool IsFullyLoaded => true;
        public bool CanDeserialize => true;
        public bool HasFlags(EPackageFlags flags) => Summary.PackageFlags.HasFlag(flags);
        public int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal) => -1;
        public ResolvedObject? ResolvePackageIndex(FPackageIndex? index) =>
            index != null && imports.TryGetValue(index.Index, out var resolved) ? resolved : null;
    }

    private sealed class FixtureObject(IPackage package, string name, UObject? value = null) : ResolvedObject(package)
    {
        public override FName Name => new(name);
        public override Lazy<UObject>? Object => value != null ? new Lazy<UObject>(value) : null;
    }
}
