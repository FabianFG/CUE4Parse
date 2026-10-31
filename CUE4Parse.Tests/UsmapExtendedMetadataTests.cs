using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.MappingsProvider.Jmap;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.Tests;

// Writes version 6 usmaps the way Dumper-7's MappingGenerator does (PR #569) and checks what comes back
public class UsmapExtendedMetadataTests
{
    private const ulong EditorOnly = (ulong) EPropertyFlags.EditorOnly;
    private const byte Byte = 0, Int = 2, Float = 3, Name = 5, StructType = 9, Map = 24, Enum = 26;

    private record Prop(string Name, ushort ArrayDim, byte[] Type, ulong Flags);
    private record TypeDef(string? Owner, string Name, string? Super, uint Flags, bool IsStruct, Prop[] Props);
    private record EnumType(string? Owner, string Name, (ulong Value, string Name)[] Values);

    private sealed class Writer
    {
        private readonly List<string> _names = [];

        public int Index(string? name)
        {
            if (name == null)
                return -1;
            var index = _names.IndexOf(name);
            if (index >= 0)
                return index;
            _names.Add(name);
            return _names.Count - 1;
        }

        public byte[] Struct(string name) => [StructType, .. BitConverter.GetBytes(Index(name))];
        public byte[] EnumOf(string name) => [Enum, Byte, .. BitConverter.GetBytes(Index(name))];

        public byte[] Write(EnumType[] enums, TypeDef[] types, Func<Prop, ushort>? flagIndex = null)
        {
            var flagTable = types.SelectMany(t => t.Props).Select(p => p.Flags).Distinct().ToList();

            var enumData = new MemoryStream();
            var ew = new BinaryWriter(enumData);
            foreach (var e in enums)
            {
                ew.Write(Index(e.Owner));
                ew.Write(Index(e.Name));
                ew.Write((ushort) e.Values.Length);
                foreach (var (value, name) in e.Values)
                {
                    ew.Write(value);
                    ew.Write(Index(name));
                }
            }

            var typeData = new MemoryStream();
            var tw = new BinaryWriter(typeData);
            foreach (var t in types)
            {
                tw.Write(Index(t.Owner));
                tw.Write(Index(t.Name));
                tw.Write(Index(t.Super));
                tw.Write(t.Flags);
                var count = (uint) t.Props.Sum(p => p.ArrayDim);
                tw.Write((count & 0xFFFFFF) | ((t.IsStruct ? 1u : 0u) << 24));
                tw.Write((uint) t.Props.Length);
                var index = 0;
                foreach (var p in t.Props)
                {
                    tw.Write((ushort) index);
                    tw.Write(p.ArrayDim);
                    tw.Write(Index(p.Name));
                    tw.Write(p.Type);
                    tw.Write(flagIndex?.Invoke(p) ?? (ushort) flagTable.IndexOf(p.Flags));
                    index += p.ArrayDim;
                }
            }

            var body = new MemoryStream();
            var w = new BinaryWriter(body);
            w.Write((uint) _names.Count);
            foreach (var name in _names)
            {
                var bytes = Encoding.UTF8.GetBytes(name);
                w.Write((ushort) bytes.Length);
                w.Write(bytes);
            }
            w.Write((uint) flagTable.Count);
            flagTable.ForEach(w.Write);
            w.Write((uint) enums.Length);
            w.Write(enumData.ToArray());
            w.Write((uint) types.Length);
            w.Write(typeData.ToArray());
            var payload = body.ToArray();

            var file = new MemoryStream();
            var fw = new BinaryWriter(file);
            fw.Write((ushort) 0x30C4);
            fw.Write((byte) EUsmapVersion.ExtendedMetadata);
            WriteUtf8("Dumper-7");
            WriteUtf8("7.0.1");
            fw.Write(1761350400L);
            fw.Write((uint) EUsmapSource.Runtime);
            fw.Write(0); // no versioning
            fw.Write((byte) EUsmapCompressionMethod.None);
            fw.Write((uint) payload.Length);
            fw.Write((uint) payload.Length);
            fw.Write(payload);
            return file.ToArray();

            void WriteUtf8(string text)
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                fw.Write(bytes.Length);
                fw.Write(bytes);
            }
        }
    }

    private static (UsmapParser Usmap, TypeMappings Mappings) Sample()
    {
        var w = new Writer();
        EnumType[] enums =
        [
            new("/Script/Foo", "EMyEnum", [(0, "EMyEnum::A"), (5, "EMyEnum::B")]),
            new(null, "ENoOwner", [(1, "ENoOwner::X")]),
        ];
        TypeDef[] types =
        [
            new("/Script/Foo", "Base", "Object", 0x00000001, false,
            [
                new("A", 1, [Int], 0x5),
                new("EdA", 1, [Int], EditorOnly | 0x1),
                new("B", 3, [Float], 0),
                new("EdTail", 2, [Name], EditorOnly),
            ]),
            new("/Script/Foo", "Derived", "Base", 0x10000000, false,
            [
                new("EdArr", 2, [Int], EditorOnly),
                new("C", 1, w.Struct("MyStruct"), 0),
                new("D", 1, w.EnumOf("EMyEnum"), 0),
                new("Big", 300, [Int], 0),
                new("M", 1, [Map, Name, Int], 0),
            ]),
            new("/Script/Foo", "MyStruct", null, 0x10000001, true, [new("X", 1, [Float], 0)]),
            new(null, "Empty", null, 0, true, []),
        ];
        var usmap = new UsmapParser(w.Write(enums, types), "synthetic");
        return (usmap, usmap.Mappings!);
    }

    private static string Slot(Struct type, int index, bool filterEditorOnly)
        => type.TryGetValue(index, filterEditorOnly, out var info) ? $"{info.Name}[{info.Index}]" : "-";

    [Fact]
    public void HeaderAndEnumsAreRead()
    {
        var (usmap, mappings) = Sample();

        Assert.Equal(EUsmapVersion.ExtendedMetadata, usmap.Version);
        Assert.Equal(("Dumper-7", "7.0.1", 1761350400L, EUsmapSource.Runtime),
            (usmap.Metadata?.Tool, usmap.Metadata?.ToolVersion, usmap.Metadata?.CreatedAtUnix, usmap.Metadata?.Source));
        Assert.Equal("EMyEnum::B", mappings.Enums["EMyEnum"][5]);
        Assert.Equal("ENoOwner::X", mappings.Enums["ENoOwner"][1]);
    }

    [Fact]
    public void StructsKeepTheirKindFlagsAndPackage()
    {
        var (_, mappings) = Sample();
        var (b, d, s, empty) = (mappings.Types["Base"], mappings.Types["Derived"], mappings.Types["MyStruct"], mappings.Types["Empty"]);

        Assert.Equal((7, 305, 1, 0), (b.PropertyCount, d.PropertyCount, s.PropertyCount, empty.PropertyCount));
        Assert.Equal((true, true, false, false), (b.IsClass, d.IsClass, s.IsClass, empty.IsClass));
        Assert.Equal((1u, 0x10000000u, 0x10000001u), (b.Flags, d.Flags, s.Flags));
        Assert.Equal(("/Script/Foo", null), (b.Package, empty.Package));
    }

    [Fact]
    public void PropertiesKeepTheirFlagsArraySizesAndTypes()
    {
        var (_, mappings) = Sample();
        var (b, d) = (mappings.Types["Base"], mappings.Types["Derived"]);

        Assert.Equal((EPropertyFlags) 0x5, b.Properties[0].PropertyFlags);
        Assert.Equal((EPropertyFlags) (EditorOnly | 0x1), b.Properties[1].PropertyFlags);
        Assert.Equal(("Big", 299, 300), (d.Properties[303].Name, d.Properties[303].Index, d.Properties[303].ArraySize));
        // reading the flag index after each type keeps the following types aligned
        Assert.Equal("MyStruct", d.Properties[2].MappingType.StructType);
        Assert.Equal("EMyEnum", d.Properties[3].MappingType.EnumName);
        Assert.Equal("IntProperty", d.Properties[304].MappingType.ValueType?.Type);
    }

    [Fact]
    public void FilteringEditorOnlyPropertiesCompactsSlotsAcrossTheSuperChain()
    {
        var (_, mappings) = Sample();
        var d = mappings.Types["Derived"];

        Assert.Equal(312, d.CountProperties(true));
        Assert.Equal(307, d.CountProperties(true, filterEditorOnly: true));
        var filtered = Enumerable.Range(0, 308).Where(i => i < 3 || i >= 300).Select(i => Slot(d, i, true));
        Assert.Equal(["C[0]", "D[0]", "Big[0]", "Big[298]", "Big[299]", "M[0]", "A[0]", "B[0]", "B[1]", "B[2]", "-"], filtered);
    }

    [Fact]
    public void ParallelFilteredLookupsAgree()
    {
        var w = new Writer();
        var chain = Enumerable.Range(0, 500).Select(k => new TypeDef(null, "S" + k, k > 0 ? "S" + (k - 1) : null, 0, true,
            [new Prop("E" + k, 1, [Int], EditorOnly), new Prop("V" + k, 1, [Int], 0)])).ToArray();
        var top = new UsmapParser(w.Write([], chain), "chain").Mappings!.Types["S499"];

        var errors = 0;
        Parallel.For(0, 32, _ =>
        {
            for (var i = 0; i < 500; i += 7)
            {
                if (!top.TryGetValue(i, true, out var info) || info.Name != "V" + (499 - i))
                    Interlocked.Increment(ref errors);
            }
        });
        Assert.Equal(0, errors);
    }

    [Fact]
    public void InvalidFlagIndexThrows()
    {
        var w = new Writer();
        var bytes = w.Write([], [new TypeDef(null, "Bad", null, 0, true, [new Prop("A", 1, [Int], 0)])], _ => 7);

        Assert.Throws<ParserException>(() => new UsmapParser(bytes, "bad"));
    }

    [Fact]
    public void JmapReadsPropertyFlagsAndClassKind()
    {
        const string json = """
        {
          "objects": {
            "/Script/Foo.Thing": {
              "type": "Class",
              "properties": [
                { "name": "Editor", "type": "IntProperty", "array_dim": 1, "flags": "CPF_Edit | CPF_EditorOnly | 0x4000000000000000" },
                { "name": "Plain", "type": "IntProperty", "array_dim": 1, "flags": "" },
                { "name": "Numeric", "type": "IntProperty", "array_dim": 1, "flags": 34359738368 }
              ]
            },
            "/Script/Foo.Data": { "type": "ScriptStruct", "properties": [] }
          }
        }
        """;
        var mappings = new JmapParser(Encoding.UTF8.GetBytes(json)).Mappings!;
        var thing = mappings.Types["Thing"];

        Assert.Equal(EPropertyFlags.Edit | EPropertyFlags.EditorOnly | (EPropertyFlags) 0x4000000000000000, thing.Properties[0].PropertyFlags);
        Assert.Equal(EPropertyFlags.None, thing.Properties[1].PropertyFlags);
        Assert.Equal(EPropertyFlags.EditorOnly, thing.Properties[2].PropertyFlags);
        Assert.Equal((true, false), (thing.IsClass, mappings.Types["Data"].IsClass));
        Assert.Equal(1, thing.CountProperties(false, filterEditorOnly: true));
    }

    [Fact]
    public void ReflectedStructsKnowTheirKindAndOlderMappingsDont()
    {
        var reflected = new SerializedStruct(null, new UScriptStruct { Name = "Reflected", ChildProperties = [], StructFlags = EStructFlags.STRUCT_Atomic });
        Assert.Equal((false, (uint) EStructFlags.STRUCT_Atomic), (reflected.IsClass, reflected.Flags));

        var older = new Struct(null, "Old", null, new Dictionary<int, PropertyInfo>(), 0);
        Assert.Null(older.IsClass);
    }
}
