using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.Tests;

public class MappingsSchemaTests
{
    private static UScriptStruct ReflectedStruct() => new()
    {
        Name = "FixtureStruct",
        ChildProperties =
        [
            new FIntProperty { Name = new FName("EditorValue"), ArrayDim = 1, PropertyFlags = EPropertyFlags.EditorOnly },
            new FIntProperty { Name = new FName("Values"), ArrayDim = 2 },
            new FIntProperty { Name = new FName("Last"), ArrayDim = 1 },
        ],
    };

    [Fact]
    public void ReflectedFixedArraysTakeOneSlotPerElement()
    {
        var schema = new SerializedStruct(null, ReflectedStruct());

        Assert.Equal(4, schema.PropertyCount);
        Assert.True(schema.TryGetValue(1, out var first));
        Assert.Equal(("Values", 0), (first.Name, first.Index));
        Assert.True(schema.TryGetValue(2, out var second));
        Assert.Equal(("Values", 1), (second.Name, second.Index));
        Assert.True(schema.TryGetValue(3, out var last));
        Assert.Equal("Last", last.Name);
    }

    [Fact]
    public void FilteringEditorOnlyPropertiesKeepsEveryArraySlot()
    {
        var schema = new SerializedStruct(null, ReflectedStruct());

        Assert.Equal(3, schema.CountProperties(false, filterEditorOnly: true));
        Assert.True(schema.TryGetValue(0, true, out var first));
        Assert.Equal(("Values", 0), (first.Name, first.Index));
        Assert.True(schema.TryGetValue(1, true, out var second));
        Assert.Equal(("Values", 1), (second.Name, second.Index));
        Assert.True(schema.TryGetValue(2, true, out var last));
        Assert.Equal("Last", last.Name);
    }

    [Fact]
    public void ClonedUsmapReaderKeepsItsNameTable()
    {
        // name table with one entry, followed by a reference to it
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(1));
        data.Add(3);
        data.AddRange(Encoding.UTF8.GetBytes("abc"));
        data.AddRange(BitConverter.GetBytes(0));

        var reader = new FUsmapReader(new FByteArchive("usmap", data.ToArray()), EUsmapVersion.Initial);
        var clone = (FUsmapReader) reader.Clone();

        Assert.Equal(reader.Position, clone.Position);
        Assert.Equal("abc", clone.ReadName());
        Assert.Equal("abc", reader.ReadName());
    }
}
