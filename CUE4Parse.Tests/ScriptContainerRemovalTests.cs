using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.Tests;

public class ScriptContainerRemovalTests
{
    private static FAssetArchive Archive(params int[] values)
    {
        var bytes = new byte[values.Length * sizeof(int)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return new FAssetArchive(new FByteArchive("container", bytes), null);
    }

    private static int[] Ints(IEnumerable<FPropertyTagType?> values) => values.Select(v => Assert.IsType<IntProperty>(v).Value).ToArray();

    private static FPropertyTagData IntMap => new() { Type = "MapProperty", InnerType = "IntProperty", ValueType = "IntProperty" };
    private static FPropertyTagData IntSet => new() { Type = "SetProperty", InnerType = "IntProperty" };

    [Fact]
    public void MapKeepsTheKeysItRemoves()
    {
        // removes keys 7 and 9 from the defaults, then adds 13 -> 130
        var Ar = Archive(2, 7, 9, 1, 13, 130);
        var map = new UScriptMap(Ar, IntMap, ReadType.NORMAL);

        Assert.Equal([7, 9], Ints(map.KeysToRemove));
        Assert.False(map.ReplacesDefaults);
        var (key, value) = Assert.Single(map.Properties);
        Assert.Equal(13, Assert.IsType<IntProperty>(key).Value);
        Assert.Equal(130, Assert.IsType<IntProperty>(value).Value);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void MapReplacingItsDefaultsIsFlagged()
    {
        // -1 means the serialized entries replace the defaults instead of patching them
        var Ar = Archive(-1, 1, 13, 130);
        var map = new UScriptMap(Ar, IntMap, ReadType.NORMAL);

        Assert.True(map.ReplacesDefaults);
        Assert.Empty(map.KeysToRemove);
        Assert.Single(map.Properties);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void MapWithoutRemovalsPatchesItsDefaults()
    {
        var Ar = Archive(0, 1, 13, 130);
        var map = new UScriptMap(Ar, IntMap, ReadType.NORMAL);

        Assert.False(map.ReplacesDefaults);
        Assert.Empty(map.KeysToRemove);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void RawMapsHaveNoRemovalPrefix()
    {
        var Ar = Archive(1, 13, 130);
        var map = new UScriptMap(Ar, IntMap, ReadType.RAW);

        Assert.Empty(map.KeysToRemove);
        Assert.Single(map.Properties);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void SetKeepsTheElementsItRemoves()
    {
        // removes 4 and 5 from the defaults, then adds 6
        var Ar = Archive(2, 4, 5, 1, 6);
        var set = new UScriptSet(Ar, IntSet, ReadType.NORMAL);

        Assert.Equal([4, 5], Ints(set.ElementsToRemove));
        Assert.Equal([6], Ints(set.Properties));
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Fact]
    public void MapKeepsTheStructKeysItRemoves()
    {
        // removes the key {1,2,3,4}, then adds {5,6,7,8} -> 9
        var Ar = Archive(1, 1, 2, 3, 4, 1, 5, 6, 7, 8, 9);
        var tagData = new FPropertyTagData
        {
            Type = "MapProperty", InnerType = "StructProperty", ValueType = "IntProperty",
            InnerTypeData = new FPropertyTagData { Type = "StructProperty", StructType = "Guid" },
        };
        var map = new UScriptMap(Ar, tagData, ReadType.NORMAL);

        var removed = Assert.IsType<FGuid>(Assert.IsType<StructProperty>(Assert.Single(map.KeysToRemove)).Value?.StructType);
        Assert.Equal(new FGuid(1, 2, 3, 4), removed);
        var (key, value) = Assert.Single(map.Properties);
        Assert.Equal(new FGuid(5, 6, 7, 8), Assert.IsType<FGuid>(Assert.IsType<StructProperty>(key).Value?.StructType));
        Assert.Equal(9, Assert.IsType<IntProperty>(value).Value);
        Assert.Equal(Ar.Length, Ar.Position);
    }

    [Theory]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, int.MaxValue)]
    public void MapCountsLargerThanTheArchiveThrowBeforeAllocating(int removed, int entries)
    {
        var Ar = Archive(removed, entries, 1, 2);
        Assert.Throws<ParserException>(() => new UScriptMap(Ar, IntMap, ReadType.NORMAL));
    }

    [Theory]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, int.MaxValue)]
    public void SetCountsLargerThanTheArchiveThrowBeforeAllocating(int removed, int elements)
    {
        var Ar = Archive(removed, elements, 1, 2);
        Assert.Throws<ParserException>(() => new UScriptSet(Ar, IntSet, ReadType.NORMAL));
    }

    [Fact]
    public void RawSetsHaveNoRemovalPrefix()
    {
        var Ar = Archive(1, 6);
        var set = new UScriptSet(Ar, IntSet, ReadType.RAW);

        Assert.Empty(set.ElementsToRemove);
        Assert.Equal([6], Ints(set.Properties));
        Assert.Equal(Ar.Length, Ar.Position);
    }
}
