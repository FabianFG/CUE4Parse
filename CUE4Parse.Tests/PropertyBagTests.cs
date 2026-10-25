using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
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

    private sealed class BagStream(EGame game)
    {
        private readonly List<string> _names = ["None"];
        private readonly List<Action<Stream>> _descs = [];
        private FAssetArchive? _archive;

        public readonly Stream Raw = new();
        public long Length => _archive!.Length;
        public long Position => _archive!.Position;

        private int Name(string name)
        {
            if (!_names.Contains(name)) _names.Add(name);
            return _names.IndexOf(name);
        }

        public BagStream Desc(string name, EPropertyBagPropertyType type,
            EPropertyBagContainerType container = EPropertyBagContainerType.None, EPropertyBagPropertyType keyType = EPropertyBagPropertyType.None)
        {
            var nameIndex = Name(name);
            _descs.Add(stream =>
            {
                stream.Int(0).Bytes(new byte[16]).Int(nameIndex).Int(0).Byte((byte) type);
                if (game >= EGame.GAME_UE5_3) // nested containers, a list of types
                {
                    if (container == EPropertyBagContainerType.None) stream.Byte(0);
                    else stream.Byte(1).Byte((byte) container);
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

        public FInstancedPropertyBag Read()
        {
            _archive = new FAssetArchive(new FByteArchive("bag", Raw.ToArray(), new VersionContainer(game)), new FixturePackage(_names));
            return new FInstancedPropertyBag(_archive);
        }

        public void AssertFullyRead() => Assert.Equal(_archive!.Length, _archive.Position);
    }

    private sealed class FixturePackage(List<string> names) : IPackage
    {
        public string Name { get; set; } = "BagFixture";
        public IFileProvider? Provider => null;
        public string? ArchiveName => null;
        public TypeMappings? Mappings { get; } = new();
        public FPackageFileSummary Summary { get; } = new() { PackageFlags = EPackageFlags.PKG_UnversionedProperties };
        public FNameEntrySerialized[] NameMap => names.Select(name => new FNameEntrySerialized(name)).ToArray();
        public int ImportMapLength => 0;
        public int ExportMapLength => 0;
        public Lazy<UObject>[] ExportsLazy => [];
        public bool IsFullyLoaded => true;
        public bool CanDeserialize => true;
        public bool HasFlags(EPackageFlags flags) => Summary.PackageFlags.HasFlag(flags);
        public int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal) => -1;
        public ResolvedObject? ResolvePackageIndex(FPackageIndex? index) => null;
    }
}
