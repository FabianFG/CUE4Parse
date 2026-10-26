using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.RigVM;
using CUE4Parse.UE4.Objects.StructUtils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.Tests;

public class PropertyBagTests
{
    [Fact]
    public void SmallIntegerTypesFromUE58AreReadWithTheirSize()
    {
        var bag = new BagStream(EGame.GAME_UE5_8)
            .Desc("A", EPropertyBagPropertyType.Int8)
            .Desc("B", EPropertyBagPropertyType.UInt16)
            .Desc("C", EPropertyBagPropertyType.Float)
            .Desc("D", EPropertyBagPropertyType.Int16)
            .Values(4, values => values.SByte(-7).UShort(65000).Float(1.5f).Short(-300));

        var result = bag.Read();

        Assert.Equal(-7, Value<sbyte>(result, "A"));
        Assert.Equal(65000, Value<ushort>(result, "B"));
        Assert.Equal(1.5f, Value<float>(result, "C"));
        Assert.Equal(-300, Value<short>(result, "D"));
        bag.AssertFullyRead();
    }

    [Fact]
    public void OlderBagsNumberUInt32RightAfterSoftClass()
    {
        var bag = new BagStream(EGame.GAME_UE5_7)
            .Desc("A", (EPropertyBagPropertyType) 16)
            .Desc("B", EPropertyBagPropertyType.Float)
            .Values(2, values => values.UInt(4_000_000_000).Float(2.5f));

        var result = bag.Read();

        Assert.Equal(EPropertyBagPropertyType.UInt32, result.PropertyDescs[0].ValueType);
        Assert.Equal(4_000_000_000, Value<uint>(result, "A"));
        Assert.Equal(2.5f, Value<float>(result, "B"));
        bag.AssertFullyRead();
    }

    [Fact]
    public void SetsAndMapsAreReadAsContainers()
    {
        var bag = new BagStream(EGame.GAME_UE5_8)
            .Desc("Set", EPropertyBagPropertyType.Int32, EPropertyBagContainerType.Set)
            .Desc("Map", EPropertyBagPropertyType.Float, EPropertyBagContainerType.Map, keyType: EPropertyBagPropertyType.Int32)
            .Values(2, values => values
                .Int(0).Int(2).Int(11).Int(22)
                .Int(0).Int(1).Int(3).Float(0.5f));

        var result = bag.Read();

        var set = Assert.IsType<SetProperty>(Tag(result, "Set")).Value!;
        Assert.Equal([11, 22], set.Properties.Select(value => value.GetValue<int>()));
        var entry = Assert.Single(Assert.IsType<MapProperty>(Tag(result, "Map")).Value!.Properties);
        Assert.Equal((3, 0.5f), (entry.Key.GetValue<int>(), entry.Value!.GetValue<float>()));
        bag.AssertFullyRead();
    }

    [Fact]
    public void UE52BagsStoreASingleContainerType()
    {
        var bag = new BagStream(EGame.GAME_UE5_2)
            .Desc("Array", EPropertyBagPropertyType.Int32, EPropertyBagContainerType.Array)
            .Values(1, values => values.Int(2).Int(7).Int(8));

        var result = bag.Read();

        var array = Assert.IsType<ArrayProperty>(Tag(result, "Array")).Value!;
        Assert.Equal([7, 8], array.Properties.Select(value => value.GetValue<int>()));
        bag.AssertFullyRead();
    }

    [Fact]
    public void ValuesThatRunPastTheirSizeAreDropped()
    {
        // The descriptor says Int64, but only four bytes were written
        var bag = new BagStream(EGame.GAME_UE5_8)
            .Desc("A", EPropertyBagPropertyType.Int64)
            .Values(1, values => values.Int(5));
        bag.Raw.Int(0);

        var result = bag.Read();

        Assert.Empty(result.Value.Properties);
        Assert.Equal(bag.Length - 4, bag.Position);
    }

    [Fact]
    public void ValuesShorterThanTheirSizeAreKept()
    {
        var bag = new BagStream(EGame.GAME_UE5_8)
            .Desc("A", EPropertyBagPropertyType.Int32)
            .Values(1, values => values.Int(5).Int(0)); // four bytes more than the value needs

        var result = bag.Read();

        Assert.Equal(5, Value<int>(result, "A"));
        bag.AssertFullyRead();
    }

    [Fact]
    public void DescriptorsUECantCreateAPropertyForTakeNoSchemaSlot()
    {
        // Before 5.8 an object without a class gets no property, so B and C are the first two values
        var objects = new BagStream(EGame.GAME_UE5_7)
            .Desc("A", EPropertyBagPropertyType.Object)
            .Desc("B", EPropertyBagPropertyType.Int32)
            .Desc("C", EPropertyBagPropertyType.Int32)
            .Values(2, values => values.Int(11).Int(22));

        var result = objects.Read();
        Assert.Equal((11, 22), (Value<int>(result, "B"), Value<int>(result, "C")));
        objects.AssertFullyRead();

        // Descriptors without a type and repeated names are dropped as well
        var sanitized = new BagStream(EGame.GAME_UE5_8)
            .Desc("X", EPropertyBagPropertyType.None)
            .Desc("A", EPropertyBagPropertyType.Int32)
            .Desc("A", EPropertyBagPropertyType.Float)
            .Desc("B", EPropertyBagPropertyType.Int32)
            .Values(2, values => values.Int(5).Int(6));

        result = sanitized.Read();
        Assert.Equal((5, 6), (Value<int>(result, "A"), Value<int>(result, "B")));
        sanitized.AssertFullyRead();
    }

    [Theory]
    [InlineData(EGame.GAME_UE5_8, "UInt32", EPropertyBagPropertyType.UInt32)]
    [InlineData(EGame.GAME_UE5_7, "Int8", EPropertyBagPropertyType.Int8)]
    public void MappingsDecideWhichNumberingTheTypesUse(EGame game, string sixteen, EPropertyBagPropertyType expected)
    {
        var mappings = new TypeMappings(new Dictionary<string, Struct>(), new Dictionary<string, Dictionary<long, string>>
        {
            ["EPropertyBagPropertyType"] = new() { [15] = "EPropertyBagPropertyType::SoftClass", [16] = $"EPropertyBagPropertyType::{sixteen}" },
        });
        var bag = new BagStream(game, mappings)
            .Desc("A", (EPropertyBagPropertyType) 16)
            .Values(1, values => values.Bytes(expected == EPropertyBagPropertyType.Int8 ? [0xF9] : BitConverter.GetBytes(7u)));

        var result = bag.Read();

        Assert.Equal(expected, result.PropertyDescs[0].ValueType);
        bag.AssertFullyRead();
    }

    [Fact]
    public void NestedContainersWrapFromTheOutside()
    {
        var bag = new BagStream(EGame.GAME_UE5_8)
            .Desc("Arrays", EPropertyBagPropertyType.Int32, containers: [EPropertyBagContainerType.Array, EPropertyBagContainerType.Array])
            .Desc("MapOfArrays", EPropertyBagPropertyType.Int32, keyType: EPropertyBagPropertyType.Int32,
                containers: [EPropertyBagContainerType.Map, EPropertyBagContainerType.Array])
            .Values(2, values => values
                .Int(2).Int(2).Int(1).Int(2).Int(1).Int(3) // [[1, 2], [3]]
                .Int(0).Int(1).Int(7).Int(2).Int(4).Int(5)); // {7: [4, 5]}

        var result = bag.Read();

        var arrays = Assert.IsType<ArrayProperty>(Tag(result, "Arrays")).Value!.Properties;
        Assert.Equal([[1, 2], [3]], arrays.Select(inner => inner.GetValue<UScriptArray>()!.Properties.Select(value => value.GetValue<int>()).ToArray()));
        var entry = Assert.Single(Assert.IsType<MapProperty>(Tag(result, "MapOfArrays")).Value!.Properties);
        Assert.Equal(7, entry.Key.GetValue<int>());
        Assert.Equal([4, 5], entry.Value!.GetValue<UScriptArray>()!.Properties.Select(value => value.GetValue<int>()));
        bag.AssertFullyRead();
    }

    [Fact]
    public void StructAndEnumDescriptorsUseTheTypeTheyPointAt()
    {
        var mappings = new TypeMappings(
            new Dictionary<string, Struct>
            {
                ["FixtureStruct"] = new(null, "FixtureStruct", null,
                    new Dictionary<int, PropertyInfo> { [0] = new(0, "Count", new PropertyType("IntProperty")) }, 1),
            },
            new Dictionary<string, Dictionary<long, string>> { ["EFixture"] = new() { [0] = "A", [1] = "B" } });
        var bag = new BagStream(EGame.GAME_UE5_8, mappings);
        bag.Imports[-1] = new FixtureObject(bag.Package, "FixtureStruct");
        bag.Imports[-2] = new FixtureObject(bag.Package, "EFixture");
        bag.Desc("Struct", EPropertyBagPropertyType.Struct, valueTypeObject: -1)
            .Desc("Enum", EPropertyBagPropertyType.Enum, valueTypeObject: -2)
            .Values(2, values => values.UShort(0x100 | 1 << 9).Int(9).Byte(1));

        var result = bag.Read();

        Assert.Equal(9, Assert.IsType<FStructFallback>(Tag(result, "Struct").GetValue<FScriptStruct>()!.StructType).Get<int>("Count"));
        Assert.Equal("EFixture::B", Tag(result, "Enum").GetValue<FName>().Text);
        bag.AssertFullyRead();
    }

    [Theory]
    [InlineData(FAnimObjectVersion.Type.SerializeRigVMRegisterArrayState - 1, 0)]
    [InlineData(FAnimObjectVersion.Type.SerializeRigVMRegisterArrayState, 1)]
    [InlineData(FAnimObjectVersion.Type.SerializeRigVMRegisterDynamicState, 2)]
    public void RigVMRegistersReadTheFlagsTheirVersionHas(FAnimObjectVersion.Type version, int flags)
    {
        var bag = new BagStream(EGame.GAME_UE4_25, customVersions: new FCustomVersionContainer([new FCustomVersion(FAnimObjectVersion.GUID, (int) version)]));
        var data = new Stream().Byte(0).UInt(16).UShort(4).UShort(1).UShort(0).UShort(1).Byte(0).UShort(0).Int(bag.Name("Register")).Int(0).Int(-1);
        for (var i = 0; i < flags; i++) data.Int(1);

        var Ar = bag.Archive(data.ToArray());
        var register = new FRigVMRegister(Ar);

        Assert.Equal("Register", register.Name.Text);
        Assert.Equal((flags >= 1, flags >= 2), (register.bIsArray, register.bIsDynamic));
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void OlderRigVMRegisterOffsetsStoreTheStructPath()
    {
        var bag = new BagStream(EGame.GAME_UE4_25, customVersions: new FCustomVersionContainer([new FCustomVersion(FReleaseObjectVersion.GUID,
            (int) FReleaseObjectVersion.Type.SerializeRigVMOffsetSegmentPaths - 1)]));
        var data = new Stream().Int(1).Int(8).Byte(0).Int(bag.Name("FVector")).Int(0).Int(bag.Name("/Script/CoreUObject.Vector")).Int(0).UShort(12);

        var Ar = bag.Archive(data.ToArray());
        var offset = new FRigVMRegisterOffset(Ar);

        Assert.Equal("/Script/CoreUObject.Vector", offset.ScriptStructPath?.Text);
        Assert.Null(offset.ScriptStruct);
        Assert.Equal(12, offset.ElementSize);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    private sealed class FixtureObject(IPackage package, string name) : ResolvedObject(package)
    {
        public override FName Name => new(name);
    }

    private static FPropertyTagType Tag(FInstancedPropertyBag bag, string name) =>
        Assert.Single(bag.Value.Properties, property => property.Name.Text == name).Tag!;

    private static T Value<T>(FInstancedPropertyBag bag, string name) => Tag(bag, name).GetValue<T>()!;

    private sealed class Stream
    {
        private readonly List<byte> _data = [];

        public int Count => _data.Count;
        public Stream Bytes(byte[] value) { _data.AddRange(value); return this; }
        public Stream Byte(byte value) { _data.Add(value); return this; }
        public Stream SByte(sbyte value) => Byte((byte) value);
        public Stream UShort(ushort value) => Bytes(BitConverter.GetBytes(value));
        public Stream Short(short value) => Bytes(BitConverter.GetBytes(value));
        public Stream Int(int value) => Bytes(BitConverter.GetBytes(value));
        public Stream UInt(uint value) => Bytes(BitConverter.GetBytes(value));
        public Stream Long(long value) => Bytes(BitConverter.GetBytes(value));
        public Stream Float(float value) => Bytes(BitConverter.GetBytes(value));
        public byte[] ToArray() => _data.ToArray();
    }

    private sealed class BagStream(EGame game, TypeMappings? mappings = null, FCustomVersionContainer? customVersions = null)
    {
        private readonly List<string> _names = ["None"];
        public readonly Dictionary<int, ResolvedObject> Imports = [];
        private readonly List<Action<Stream>> _descs = [];
        private FAssetArchive? _archive;

        public readonly Stream Raw = new();
        public long Length => _archive!.Length;
        public long Position => _archive!.Position;

        public int Name(string name)
        {
            if (!_names.Contains(name)) _names.Add(name);
            return _names.IndexOf(name);
        }

        public BagStream Desc(string name, EPropertyBagPropertyType type,
            EPropertyBagContainerType container = EPropertyBagContainerType.None, EPropertyBagPropertyType keyType = EPropertyBagPropertyType.None,
            EPropertyBagContainerType[]? containers = null, int valueTypeObject = 0)
        {
            var nameIndex = Name(name);
            containers ??= container == EPropertyBagContainerType.None ? [] : [container];
            _descs.Add(stream =>
            {
                stream.Int(valueTypeObject).Bytes(new byte[16]).Int(nameIndex).Int(0).Byte((byte) type);
                if (game >= EGame.GAME_UE5_3) // nested containers, a list of types
                {
                    stream.Byte((byte) containers.Length);
                    foreach (var c in containers) stream.Byte((byte) c);
                }
                else if (game >= EGame.GAME_UE5_1)
                {
                    stream.Byte((byte) container);
                }
                stream.Int(0); // no metadata
                if (game >= EGame.GAME_UE5_8)
                    stream.Long(0).Byte((byte) keyType).Int(0); // property flags, key type
            });
            return this;
        }

        // Unversioned values of the first count properties
        public BagStream Values(int count, Action<Stream> write)
        {
            var values = new Stream().UShort((ushort) (0x100 | count << 9));
            write(values);

            Raw.Int(1).Int(_descs.Count);
            foreach (var desc in _descs) desc(Raw);
            Raw.Int(values.Count).Bytes(values.ToArray());
            return this;
        }

        private FixturePackage? _package;
        public FixturePackage Package => _package ??= new FixturePackage(_names, mappings ?? new TypeMappings(), Imports);
        public FAssetArchive Archive(byte[] data) => _archive = new FAssetArchive(new FByteArchive("bag", data, new VersionContainer(game, customVersions: customVersions)), Package);

        public FInstancedPropertyBag Read()
        {
            _archive = new FAssetArchive(new FByteArchive("bag", Raw.ToArray(), new VersionContainer(game, customVersions: customVersions)), Package);
            return new FInstancedPropertyBag(_archive);
        }

        public void AssertFullyRead() => Assert.Equal(_archive!.Length, _archive.Position);
    }

    private sealed class FixturePackage(List<string> names, TypeMappings mappings, Dictionary<int, ResolvedObject> imports) : IPackage
    {
        public string Name { get; set; } = "BagFixture";
        public IFileProvider? Provider => null;
        public string? ArchiveName => null;
        public TypeMappings? Mappings => mappings;
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
}
