using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.Tests;

// Mappings built from a package's own class (SerializedStruct) must read an FEnumProperty value
// with the width of its UnderlyingProp, not always one byte.
public class SerializedEnumPropertyTests
{
    private static FEnumProperty EnumProp(FNumericProperty? underlying, int elementSize) => new()
    {
        Name = new FName("Value"),
        ElementSize = elementSize,
        Enum = new FPackageIndex(),
        UnderlyingProp = underlying,
    };

    // Reads one unversioned-style raw value; returns the read text and how many bytes were consumed
    private static (string Value, long Read) ReadRaw(PropertyType type, byte[] bytes)
    {
        var Ar = new FAssetArchive(new FByteArchive("synthetic", bytes), null);
        var tag = new FPropertyTag(Ar, new PropertyInfo(0, "Value", type), ReadType.RAW);
        return (((EnumProperty) tag.Tag!).Value.Text, Ar.Position);
    }

    [Fact]
    public void UInt16UnderlyingReadsTwoBytes()
    {
        var type = new PropertyType(EnumProp(new FUInt16Property { ElementSize = 2 }, 2));
        Assert.Equal("UInt16Property", type.InnerType?.Type);

        // 0x012C = 300, followed by a sentinel byte that must stay unread
        var (value, read) = ReadRaw(type, [0x2C, 0x01, 0xAA]);
        Assert.Equal("300", value);
        Assert.Equal(2, read);
    }

    [Fact]
    public void Int64UnderlyingReadsEightBytes()
    {
        var type = new PropertyType(EnumProp(new FInt64Property { ElementSize = 8 }, 8));
        var (value, read) = ReadRaw(type, [0x05, 0, 0, 0, 0, 0, 0, 0, 0xAA]);
        Assert.Equal("5", value);
        Assert.Equal(8, read);
    }

    [Fact]
    public void ByteUnderlyingStillReadsOneByte()
    {
        var type = new PropertyType(EnumProp(new FByteProperty { ElementSize = 1, Enum = new FPackageIndex() }, 1));
        // the inner type is a plain number: no enum of its own, so the outer enum maps the index
        Assert.Equal("ByteProperty", type.InnerType?.Type);
        Assert.Null(type.InnerType?.EnumName);
        Assert.Null(type.InnerType?.Enum);
        var (value, read) = ReadRaw(type, [0x2F, 0xAA]);
        Assert.Equal("47", value);
        Assert.Equal(1, read);
    }

    [Fact]
    public void MissingUnderlyingFallsBackToElementSize()
    {
        Assert.Null(new PropertyType(EnumProp(null, 1)).InnerType);
        Assert.Equal("IntProperty", new PropertyType(EnumProp(null, 4)).InnerType?.Type);
    }
}
