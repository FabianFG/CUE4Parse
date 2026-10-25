using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
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

    private static byte[] DnaFile(params (string Id, byte[] Data)[] layers)
    {
        var file = new BigEndianStream();
        file.Bytes("DNA"u8.ToArray()).UShort(2).UShort(5).Int(layers.Length);
        var offset = 3 + 4 + 4 + layers.Length * 16;
        foreach (var (id, layer) in layers)
        {
            file.Bytes(Encoding.ASCII.GetBytes(id)).UShort(2).UShort(5).Int(offset).Int(layer.Length);
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
    private static BigEndianStream JointGroupBehavior() => new BigEndianStream()
        .UShort(2).EmptyArrays(6)
        .UShorts(2, 3, 3).UShorts(0, 0, 1).Floats(0.5f, 1, 1)
        .UShort(9).UShort(3).Int(1)
        .UShorts(2).UShorts(0, 2, 3).UShorts(0, 1).Floats(1, 2, 4, 0, 0, 8).UShorts(0)
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
