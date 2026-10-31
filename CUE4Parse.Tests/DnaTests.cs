using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.DNA;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.Tests;

public class DnaTests
{
    [Theory]
    [InlineData(EGame.GAME_UE4_25, -1)]
    [InlineData(EGame.GAME_UE4_27, (int) FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)]
    [InlineData(EGame.GAME_UE5_7, (int) FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)]
    [InlineData(EGame.GAME_UE5_8, (int) FDNAAssetCustomVersion.Type.UserModifiableDNAConfig)]
    public void UnversionedPackagesGetTheDnaVersionOfTheirEngine(EGame game, int expected)
    {
        Assert.Equal(expected, (int) FDNAAssetCustomVersion.Get(new FByteArchive("dna", [], new VersionContainer(game))));
    }

    // RigLogic composes the axis rotations so that the first axis of the sequence applies first
    [Theory]
    [InlineData(ERotationSequence.XYZ)]
    [InlineData(ERotationSequence.XZY)]
    [InlineData(ERotationSequence.YXZ)]
    [InlineData(ERotationSequence.YZX)]
    [InlineData(ERotationSequence.ZXY)]
    [InlineData(ERotationSequence.ZYX)]
    public void EulerAnglesFollowTheirRotationSequence(ERotationSequence sequence)
    {
        var (x, y, z) = (30f, -45f, 60f);
        var axes = new Dictionary<char, Quaternion>
        {
            ['X'] = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x * MathF.PI / 180),
            ['Y'] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -y * MathF.PI / 180),
            ['Z'] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z * MathF.PI / 180),
        };
        var order = sequence.ToString();
        var expected = axes[order[2]] * axes[order[1]] * axes[order[0]];

        var actual = RigLogicEvaluator.EulerDegreesToQuat(x, y, z, sequence, 1, -1, 1);

        var dot = expected.X * actual.x + expected.Y * actual.y + expected.Z * actual.z + expected.W * actual.w;
        Assert.Equal(1f, MathF.Abs(dot), 1e-5f);
    }

    [Fact]
    public void RawPosesIncludePoseSpaceDeformers()
    {
        var dna = new FixtureDna(["A", "B"], ["root"], JointGroupBehavior().ToBehavior());

        Assert.True(dna.TryConvert(out var poses));

        var key = Assert.Single(poses.Poses[0].Keys);
        Assert.Equal((2f, 0f, 0f), (key.Location.X, key.Location.Y, key.Location.Z));
        Assert.Empty(poses.Poses[1].Keys);
    }

    // Optimized cooked DNA only keeps the definition, so there is nothing to evaluate
    [Fact]
    public void DnaWithoutJointGroupsHasNoPoses()
    {
        var dna = new FixtureDna(["A", "B"], ["root"], EmptyBehavior().ToBehavior());

        Assert.False(dna.TryConvert(out _));
    }

    // Version 0 assets hold a behavior DNA followed by a geometry DNA
    [Fact]
    public void LegacyAssetsKeepTheBehaviorAndTakeTheGeometryOfTheSecondDna()
    {
        var behavior = DnaFile(("bhvr", JointGroupBehavior().ToArray()));
        var geometry = DnaFile(("bhvr", EmptyBehavior().ToArray()), ("geom", new BigEndianStream().Int(0).ToArray()));
        var data = new List<byte>();
        data.Add(0); // no serialization control extensions
        data.AddRange(BitConverter.GetBytes(0L)); // "None", no properties
        data.AddRange(BitConverter.GetBytes(0)); // no object guid
        data.AddRange(behavior);
        data.AddRange(geometry);
        var Ar = new FAssetArchive(new FByteArchive("dna", data.ToArray(), new VersionContainer(EGame.GAME_UE5_7)), new FixturePackage());

        var asset = new UDNAAsset();
        asset.Deserialize(Ar, Ar.Length);

        Assert.Single(asset.GetBehavior()!.Joints.JointGroups);
        Assert.True(DnaAssetQueries.TryGet(asset.Layers!, "geom", out RawGeometry _));
        Assert.Equal(behavior, asset.DNAData!.Value);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void LegacyAssetsKeepAGeometryDnaThatHasMeshes()
    {
        var behavior = DnaFile(("bhvr", JointGroupBehavior().ToArray()));
        var geometry = DnaFile(("bhvr", EmptyBehavior().ToArray()), ("geom", new BigEndianStream().Int(1).Bytes(EmptyMesh()).ToArray()));
        var Ar = LegacyAsset(behavior, geometry);

        var asset = new UDNAAsset();
        asset.Deserialize(Ar, Ar.Length);

        Assert.Equal(behavior, asset.DNAData!.Value);
        Assert.Equal(geometry, asset.GeometryDNAData!.Value);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void Dna22ReadsTheIndexTable()
    {
        // Only DNA 2.1 has the section lookup table
        var file = DnaFile(2, ("bhvr", JointGroupBehavior().ToArray()));

        var doc = DnaBinaryParser.Parse(new FArchiveBigEndian(new FByteArchive("dna", file)), 0);

        Assert.True(DnaAssetQueries.TryGet(doc.Layers, "bhvr", out RawBehavior behavior));
        Assert.Single(behavior.Joints.JointGroups);
        Assert.Equal(file.Length, doc.ByteLength);
    }

    [Fact]
    public void IndexEntriesPastTheEndDontStretchTheDna()
    {
        var file = DnaFile(5, ("bhvr", JointGroupBehavior().ToArray()));
        BinaryPrimitives.WriteInt32BigEndian(file.AsSpan(3 + 4 + 4 + 12), int.MaxValue); // the layer's size

        var doc = DnaBinaryParser.Parse(new FArchiveBigEndian(new FByteArchive("dna", file)), 0);

        Assert.Equal(file.Length, doc.ByteLength);
    }

    [Fact]
    public void ArrayCountsPastTheEndThrowBeforeAllocating()
    {
        var Ar = new FArchiveBigEndian(new FByteArchive("dna", new BigEndianStream().Int(int.MaxValue).Int(0).ToArray()));

        Assert.Throws<ParserException>(() => Ar.ReadArray(() => Ar.Read<int>()));
    }

    [Fact]
    public void RawPosesUseTheDnaUnits()
    {
        // The joint group's second output is the X rotation, 0.5 radians for control A
        var dna = new FixtureDna(["A", "B"], ["root"], JointGroupBehavior(outputs: [0, 3], values: [1, 2, 4, 0.5f, 0, 0]).ToBehavior());
        var descriptor = (RawDescriptor) RuntimeHelpers.GetUninitializedObject(typeof(RawDescriptor));
        (descriptor.TranslationUnit, descriptor.RotationUnit) = (ETranslationUnit.M, ERotationUnit.Radians);
        dna.Layers!["desc"] = descriptor;

        Assert.True(dna.TryConvert(out var poses));

        var key = Assert.Single(poses.Poses[0].Keys);
        Assert.Equal(200f, key.Location.X, 3);
        Assert.Equal(MathF.Sin(0.25f), MathF.Abs(key.Rotation.X), 4);
        Assert.Equal(MathF.Cos(0.25f), key.Rotation.W, 4);
    }

    [Fact]
    public void ConfigFieldsLeftOutUseTheOuterStructsDefaults()
    {
        // UE leaves RotationSign.ZAxis out because it matches FDNAConfig's default of Positive
        var rotationSign = new FStructFallback([
            Tag("XAxis", new EnumProperty(new FName("ERotationDirection::Positive"))),
            Tag("YAxis", new EnumProperty(new FName("ERotationDirection::Positive"))),
        ]);
        var config = new FDNAConfig(new FStructFallback([Tag("RotationSign", new StructProperty(new FScriptStruct(rotationSign)))]));

        Assert.Equal((ERotationDirection.Positive, ERotationDirection.Positive, ERotationDirection.Positive),
            (config.RotationSign.XAxis, config.RotationSign.YAxis, config.RotationSign.ZAxis));
        Assert.Equal((EDirection.Left, EDirection.Front, EDirection.Up),
            (config.CoordinateSystem.XAxis, config.CoordinateSystem.YAxis, config.CoordinateSystem.ZAxis));
        Assert.Equal(ERigLogicRotationType.Quaternions, new FRigLogicConfiguration(new FStructFallback()).RotationType);
        Assert.Equal(ERigLogicCalculationType.AnyVector, new FRigLogicConfiguration().CalculationTypePerPlatform.Default);

        static FPropertyTag Tag(string name, FPropertyTagType value) =>
            new(new FName(name), new FName(value.GetType().Name), 0, 0, null, false, null, value);
    }

    private static FAssetArchive LegacyAsset(params byte[][] streams)
    {
        var data = new List<byte>();
        data.Add(0); // no serialization control extensions
        data.AddRange(BitConverter.GetBytes(0L)); // "None", no properties
        data.AddRange(BitConverter.GetBytes(0)); // no object guid
        foreach (var stream in streams)
            data.AddRange(stream);
        return new FAssetArchive(new FByteArchive("dna", data.ToArray(), new VersionContainer(EGame.GAME_UE5_7)), new FixturePackage());
    }

    // Offset, positions, texture coordinates, normals, layouts, faces, max influences, skin weights, blend shape targets
    private static byte[] EmptyMesh() => new BigEndianStream().Int(0).EmptyArrays(3 + 2 + 3 + 3 + 1).UShort(0).EmptyArrays(2).ToArray();

    private static byte[] DnaFile(params (string Id, byte[] Data)[] layers) => DnaFile(5, layers);

    private static byte[] DnaFile(ushort minor, params (string Id, byte[] Data)[] layers)
    {
        var file = new BigEndianStream();
        file.Bytes("DNA"u8.ToArray()).UShort(2).UShort(minor).Int(layers.Length);
        var offset = 3 + 4 + 4 + layers.Length * 16;
        foreach (var (id, layer) in layers)
        {
            file.Bytes(Encoding.ASCII.GetBytes(id)).UShort(2).UShort(minor).Int(offset).Int(layer.Length);
            offset += layer.Length;
        }
        foreach (var (_, layer) in layers)
            file.Bytes(layer);
        return file.ToArray();
    }

    private sealed class FixturePackage : IPackage
    {
        public string Name { get; set; } = "DnaFixture";
        public IFileProvider? Provider => null;
        public string? ArchiveName => null;
        public TypeMappings? Mappings => null;
        public FPackageFileSummary Summary { get; } = new();
        public FNameEntrySerialized[] NameMap { get; } = [new("None")];
        public int ImportMapLength => 0;
        public int ExportMapLength => 0;
        public Lazy<UObject>[] ExportsLazy => [];
        public bool IsFullyLoaded => true;
        public bool CanDeserialize => true;
        public bool HasFlags(EPackageFlags flags) => Summary.PackageFlags.HasFlag(flags);
        public int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal) => -1;
        public ResolvedObject? ResolvePackageIndex(FPackageIndex? index) => null;
    }

    // Controls A and B, PSD 2 = 0.5 * A and PSD 3 = A * B, one joint group driving the translation of joint 0
    private static BigEndianStream JointGroupBehavior(ushort[]? outputs = null, float[]? values = null) => new BigEndianStream()
        .UShort(2).EmptyArrays(6)
        .UShorts(2, 3, 3).UShorts(0, 0, 1).Floats(0.5f, 1, 1)
        .UShort(9).UShort(3).Int(1)
        .UShorts(2).UShorts(0, 2, 3).UShorts(outputs ?? [0, 1]).Floats(values ?? [1, 2, 4, 0, 0, 8]).UShorts(0)
        .EmptyArrays(3)
        .EmptyArrays(7);

    private static BigEndianStream EmptyBehavior() => new BigEndianStream()
        .UShort(0).EmptyArrays(9)
        .UShort(0).UShort(0).Int(0)
        .EmptyArrays(3)
        .EmptyArrays(7);

    private sealed class BigEndianStream
    {
        private readonly List<byte> _data = [];

        public BigEndianStream UShort(ushort value)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
            _data.AddRange(bytes.ToArray());
            return this;
        }

        public BigEndianStream Int(int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            _data.AddRange(bytes.ToArray());
            return this;
        }

        public BigEndianStream UShorts(params ushort[] values)
        {
            Int(values.Length);
            foreach (var value in values) UShort(value);
            return this;
        }

        public BigEndianStream Floats(params float[] values)
        {
            Int(values.Length);
            foreach (var value in values) Int(BitConverter.SingleToInt32Bits(value));
            return this;
        }

        public BigEndianStream EmptyArrays(int count)
        {
            for (var i = 0; i < count; i++) Int(0);
            return this;
        }

        public BigEndianStream Bytes(byte[] values)
        {
            _data.AddRange(values);
            return this;
        }

        public byte[] ToArray() => _data.ToArray();

        public RawBehavior ToBehavior() => new(new FArchiveBigEndian(new FByteArchive("bhvr", _data.ToArray())));
    }

    private sealed class FixtureDna(string[] controls, string[] joints, RawBehavior behavior) : IDnaAsset
    {
        public Dictionary<string, IRawBase>? Layers { get; } = new() { ["bhvr"] = behavior };
        public RigLogicSnapshot? Snapshot => null;
        public int GetRawControlCount() => controls.Length;
        public string[] GetRawControlNames() => controls;
        public string GetRawControlName(int index) => controls[index];
        public int GetJointCount() => joints.Length;
        public string[] GetJointNames() => joints;
        public string GetJointName(int index) => joints[index];
        public RawBehavior? GetBehavior() => behavior;
    }
}
