using CUE4Parse.GameTypes.WuWa.Objects;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.Tests.GameTypes;

public class WutheringWavesInstanceDataTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Deserialize_Foliage_KeepsInstancesSeparateFromClusterTree(int renderDataSize)
    {
        const int sentinel = 0x12345678;
        using var bytes = CreateArchive(writer =>
        {
            writer.Write(0L); // None: end of tagged properties
            writer.Write(0); // No UObject GUID
            writer.Write(0); // Empty LODData
            writer.Write(1); // Cooked
            writer.Write(true); // WuWa compressed instances
            WriteHeader(writer, new FVector(100, 200, 300), 2);
            WriteInstance(writer, new FVector(1, 2, 3), FVector.ZeroVector, true, new FVector(0.5f));
            WriteInstance(writer, new FVector(4, 5, 6), FVector.ZeroVector, false, new FVector(1, 2, 3));
            writer.Write(2); // Instance remapping array
            writer.Write(1);
            writer.Write(0);
            writer.Write(sizeof(float)); // Custom data bulk array
            writer.Write(2);
            writer.Write(0.25f);
            writer.Write(0.75f);
            writer.Write((long)renderDataSize);
            writer.Write(new byte[renderDataSize]);
            writer.Write(64); // FClusterNode bulk array, not instance matrices
            writer.Write(1);
            WriteVector(writer, new FVector(-1000)); // BoundMin
            writer.Write(-1); // FirstChild
            WriteVector(writer, new FVector(1000)); // BoundMax
            writer.Write(-1); // LastChild
            writer.Write(0); // FirstInstance
            writer.Write(1); // LastInstance
            WriteVector(writer, new FVector(0.5f)); // MinInstanceScale
            WriteVector(writer, new FVector(3)); // MaxInstanceScale
            writer.Write(sentinel);
        });
        using var ar = new SyntheticAssetArchive(bytes);
        var component = new UFoliageInstancedStaticMeshComponent();

        component.Deserialize(ar, ar.Length - sizeof(int));

        Assert.NotNull(component.PerInstanceSMData);
        Assert.Equal(2, component.PerInstanceSMData.Length);
        AssertVector(new FVector(101, 202, 303), component.PerInstanceSMData[0].TransformData.Translation);
        AssertVector(new FVector(0.5f), component.PerInstanceSMData[0].TransformData.Scale3D);
        AssertVector(new FVector(1, 2, 3), component.PerInstanceSMData[1].TransformData.Scale3D);
        Assert.Equal(new[] { 0.25f, 0.75f }, component.PerInstanceSMCustomData);
        Assert.NotNull(component.ClusterTree);
        var cluster = Assert.IsType<FClusterNode>(Assert.Single(component.ClusterTree));
        AssertVector(new FVector(-1000), cluster.BoundMin);
        AssertVector(new FVector(1000), cluster.BoundMax);
        Assert.Equal(0, cluster.FirstInstance);
        Assert.Equal(1, cluster.LastInstance);
        AssertVector(new FVector(0.5f), cluster.MinInstanceScale);
        AssertVector(new FVector(3), cluster.MaxInstanceScale);
        Assert.Equal(ar.Length - sizeof(int), ar.Position);
        Assert.Equal(sentinel, ar.Read<int>());
    }

    [Fact]
    public void Read_MixedScaleRecords_PreservesTransformsAndFollowingData()
    {
        const int sentinel = 0x12345678;
        using var ar = CreateArchive(writer =>
        {
            WriteHeader(writer, new FVector(1000, -2000, 3000), 3);
            WriteInstance(writer, new FVector(1.5f, -2.25f, 3), FVector.ZeroVector, true, new FVector(0.5f));
            WriteInstance(writer, new FVector(-4, 5, -6), FVector.ZeroVector, false, new FVector(-2, 0.25f, 3));
            WriteInstance(writer, FVector.ZeroVector, FVector.ZeroVector, true, new FVector(1));
            writer.Write(sentinel);
        });

        var instances = FCompressedInstanceData.Read(ar, ar.Length);

        Assert.Equal(3, instances.Length);
        AssertVector(new FVector(1001.5f, -2002.25f, 3003), instances[0].TransformData.Translation);
        AssertVector(new FVector(0.5f), instances[0].TransformData.Scale3D);
        AssertVector(new FVector(996, -1995, 2994), instances[1].TransformData.Translation);
        AssertVector(new FVector(-2, 0.25f, 3), instances[1].TransformData.Scale3D);
        AssertVector(new FVector(1000, -2000, 3000), instances[2].TransformData.Translation);
        AssertVector(new FVector(1), instances[2].TransformData.Scale3D);
        // Uniform records occupy 15 bytes and nonuniform records 19, with no padding byte.
        Assert.Equal(16 + 15 + 19 + 15, ar.Position);
        Assert.Equal(sentinel, ar.Read<int>());
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, -1)] // Roll: Y -> -Z
    [InlineData(0, 1, 0, 1, 0, 0, 0, 0, 1)]  // Pitch: X -> Z
    [InlineData(0, 0, 1, 1, 0, 0, 0, 1, 0)]  // Yaw: X -> Y
    public void Read_EulerXYZ_UsesRadiansAndUnrealRotationAxes(
        float roll, float pitch, float yaw, float x, float y, float z,
        float expectedX, float expectedY, float expectedZ)
    {
        using var ar = CreateArchive(writer =>
        {
            WriteHeader(writer, FVector.ZeroVector, 1);
            WriteInstance(writer, FVector.ZeroVector, new FVector(roll, pitch, yaw) * (MathF.PI / 2),
                true, new FVector(1));
        });

        var transform = Assert.Single(FCompressedInstanceData.Read(ar, ar.Length)).TransformData;

        AssertVector(new FVector(expectedX, expectedY, expectedZ), transform.TransformVector(new FVector(x, y, z)));
    }

    [Fact]
    public void Read_EmptyArray_ConsumesOnlyHeader()
    {
        using var ar = CreateArchive(writer => WriteHeader(writer, FVector.ZeroVector, 0));

        Assert.Empty(FCompressedInstanceData.Read(ar, ar.Length));
        Assert.Equal(16, ar.Position);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Read_InvalidCount_ThrowsParserException(int count)
    {
        using var ar = CreateArchive(writer => WriteHeader(writer, FVector.ZeroVector, count));

        Assert.Throws<ParserException>(() => FCompressedInstanceData.Read(ar, ar.Length));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(34)]
    public void Read_TruncatedAtExportBoundary_DoesNotReadFollowingBytes(int validPos)
    {
        // Keep the bytes in the archive to detect reads beyond this export's validPos.
        using var ar = CreateArchive(writer =>
        {
            WriteHeader(writer, FVector.ZeroVector, 1);
            WriteInstance(writer, FVector.ZeroVector, FVector.ZeroVector, false, new FVector(1, 2, 3));
        });

        Assert.Throws<ParserException>(() => FCompressedInstanceData.Read(ar, validPos));
        Assert.InRange(ar.Position, 0L, (long)validPos);
    }

    [Fact]
    public void Read_TruncatedSecondRecord_DoesNotReadFollowingBytes()
    {
        using var ar = CreateArchive(writer =>
        {
            WriteHeader(writer, FVector.ZeroVector, 2);
            WriteInstance(writer, FVector.ZeroVector, FVector.ZeroVector, false, new FVector(1, 2, 3));
            WriteInstance(writer, FVector.ZeroVector, FVector.ZeroVector, true, new FVector(1));
        });
        const int validPos = 16 + 19 + 14;

        Assert.Throws<ParserException>(() => FCompressedInstanceData.Read(ar, validPos));
        Assert.Equal(16 + 19, ar.Position);
    }

    [Fact]
    public void Read_InvalidUniformScaleFlag_ThrowsParserException()
    {
        using var ar = CreateArchive(writer =>
        {
            WriteHeader(writer, FVector.ZeroVector, 1);
            WriteHalfVector(writer, FVector.ZeroVector);
            WriteHalfVector(writer, FVector.ZeroVector);
            writer.Write((byte)2);
            WriteHalf(writer, 1);
        });

        Assert.Throws<ParserException>(() => FCompressedInstanceData.Read(ar, ar.Length));
    }

    [Theory]
    [InlineData(0, float.NaN)]
    [InlineData(0, float.PositiveInfinity)]
    [InlineData(1, float.NaN)]
    [InlineData(2, float.NegativeInfinity)]
    [InlineData(3, float.PositiveInfinity)]
    public void Read_NonfiniteTransform_ThrowsParserException(int field, float value)
    {
        var invalid = new FVector(value, 0, 0);
        using var ar = CreateArchive(writer =>
        {
            WriteHeader(writer, field == 0 ? invalid : FVector.ZeroVector, 1);
            WriteInstance(writer, field == 1 ? invalid : FVector.ZeroVector,
                field == 2 ? invalid : FVector.ZeroVector, false, field == 3 ? invalid : new FVector(1));
        });

        Assert.Throws<ParserException>(() => FCompressedInstanceData.Read(ar, ar.Length));
    }

    private static FByteArchive CreateArchive(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        write(writer);
        writer.Flush();
        return new FByteArchive("Synthetic WuWa instances", stream.ToArray(), new VersionContainer(EGame.GAME_WutheringWaves));
    }

    private static void WriteHeader(BinaryWriter writer, FVector origin, int count)
    {
        WriteVector(writer, origin);
        writer.Write(count);
    }

    private static void WriteVector(BinaryWriter writer, FVector value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    private static void WriteInstance(BinaryWriter writer, FVector translation, FVector angles, bool uniform, FVector scale)
    {
        WriteHalfVector(writer, translation);
        WriteHalfVector(writer, angles);
        writer.Write(uniform);
        if (uniform)
            WriteHalf(writer, scale.X);
        else
            WriteHalfVector(writer, scale);
    }

    private static void WriteHalfVector(BinaryWriter writer, FVector value)
    {
        WriteHalf(writer, value.X);
        WriteHalf(writer, value.Y);
        WriteHalf(writer, value.Z);
    }

    private static void WriteHalf(BinaryWriter writer, float value) => writer.Write(BitConverter.HalfToUInt16Bits((Half)value));

    private static void AssertVector(FVector expected, FVector actual)
    {
        const float tolerance = 0.001f;
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.Z - actual.Z), 0, tolerance);
    }

    // This fixture has no properties and only needs the None name; no game package is required.
    private sealed class SyntheticAssetArchive(FArchive archive) : FAssetArchive(archive, null)
    {
        public override FName ReadFName()
        {
            Assert.Equal(0L, Read<long>());
            return new FName("None");
        }
    }
}
