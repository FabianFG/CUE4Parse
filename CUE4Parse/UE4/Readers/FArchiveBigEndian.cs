using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Readers;

public class FArchiveBigEndian : FArchive
{
    private readonly FArchive _baseArchive;

    public FArchiveBigEndian(FArchive baseArchive) : base(baseArchive.Versions)
    {
        _baseArchive = baseArchive;
    }

    private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private record struct LayoutFixup(ushort Offset, ushort Size, ushort Count);
    private static readonly ConcurrentDictionary<Type, LayoutFixup[]> Layouts = new();

    private static readonly Dictionary<Type, int> PrimitiveTypes = new()
    {
        [typeof(byte)] = 1,
        [typeof(sbyte)] = 1,
        [typeof(bool)] = 1,

        [typeof(short)] = 2,
        [typeof(ushort)] = 2,
        [typeof(char)] = 2,
        [typeof(Half)] = 2,

        [typeof(int)] = 4,
        [typeof(uint)] = 4,
        [typeof(float)] = 4,

        [typeof(long)] = 8,
        [typeof(ulong)] = 8,
        [typeof(double)] = 8,
        [typeof(nint)] = 8,
        [typeof(nuint)] = 8,
    };

    public sealed override T Read<T>()
    {
        var size = Unsafe.SizeOf<T>();
        Span<byte> span = stackalloc byte[size];
        Read(span);

        var layout = Layouts.GetOrAdd(typeof(T), BuildLayout(typeof(T)));
        foreach (var field in layout)
        {
            ReverseEndian(span.Slice(field.Offset, field.Size * field.Count), field.Size);
        }
        return Unsafe.ReadUnaligned<T>(ref span[0]);
    }

    public override int Read(byte[] buffer, int offset, int count) => _baseArchive.Read(buffer, offset, count);
    public override string ReadString() => Encoding.UTF8.GetString(ReadArray<byte>());
    public override long Seek(long offset, SeekOrigin origin) => _baseArchive.Seek(offset, origin);

    public sealed override T[] ReadArray<T>(int length)
    {
        var size = Unsafe.SizeOf<T>();
        var readLength = size * length;
        CheckReadSize(readLength);

        if (PrimitiveTypes.TryGetValue(typeof(T), out _))
        {
            var buffer = ReadBytes(readLength);
            ReverseEndian(buffer, size);
            var result = new T[length];
            if (length > 0) Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref buffer[0], (uint)(readLength));
            return result;
        }

        var result1 = new T[length];
        for (var i = 0; i < length; i++)
            result1[i] = Read<T>();
        return result1;
    }

    static void ReverseEndian(Span<byte> span, int size)
    {
        switch (size)
        {
            case 1:
                return;
            case 2:
            {
                var span2 = span.Cast<byte, ushort>();
                for (int i = 0; i < span2.Length; i++)
                {
                    span2[i] = BinaryPrimitives.ReverseEndianness(span2[i]);
                }

                break;
            }
            case 4:
            {
                var span4 = span.Cast<byte, uint>();
                for (int i = 0; i < span4.Length; i++)
                {
                    span4[i] = BinaryPrimitives.ReverseEndianness(span4[i]);
                }

                break;
            }
            case 8:
            {
                var span8 = span.Cast<byte, ulong>();
                for (int i = 0; i < span8.Length; i++)
                {
                    span8[i] = BinaryPrimitives.ReverseEndianness(span8[i]);
                }

                break;
            }
            default:
                throw new NotSupportedException($"Unsupported size {size} for reversing endianness");
        }
    }

    public override bool CanSeek => _baseArchive.CanSeek;
    public override long Length => _baseArchive.Length;
    public override string Name => _baseArchive.Name;
    public override long Position
    {
        get => _baseArchive.Position;
        set => _baseArchive.Position = value;
    }

    public override object Clone() => new FArchiveBigEndian(_baseArchive);

    private static LayoutFixup[] BuildLayout(Type type)
    {
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        if (PrimitiveTypes.TryGetValue(type, out int primitiveSize))
        {
            if (primitiveSize == 1) return [];
            return [new LayoutFixup(0, (ushort)primitiveSize, 1)];
        }

        List<LayoutFixup> layoutFixups = [];
        var inline = type.GetCustomAttribute<InlineArrayAttribute>();
        if (inline != null)
        {
            var element = type.GetFields(FieldFlags).Single().FieldType;

            if (PrimitiveTypes.TryGetValue(element, out var elemsize))
            {
                if (primitiveSize == 1) return [];
                return [new LayoutFixup(0, (ushort) elemsize, (ushort) inline.Length)];
            }
            else
            {
                var temp = Layouts.GetOrAdd(element, BuildLayout(element));
                var tempOffset = 0;
                var tempsize = Marshal.SizeOf(element);
                for (var i = 0; i < inline.Length; i++)
                {
                    foreach (var fix in temp)
                    {
                        layoutFixups.Add(fix with { Offset = (ushort)(fix.Offset + tempOffset) });
                    }
                    tempOffset += tempsize;
                }
            }

            return CompactLayout(layoutFixups);
        }

        foreach (var field in type.GetFields(FieldFlags).OrderBy(x => x.MetadataToken))
        {
            int offset = (int)Marshal.OffsetOf(type, field.Name);
            Type fieldType = field.FieldType;
            if (!fieldType.IsValueType)
                throw new ParserException($"FArchiveBigEndian can't read reference type {fieldType} in {type}");

            if (PrimitiveTypes.TryGetValue(fieldType, out int size))
            {
                if (size == 1) continue;
                layoutFixups.Add(new LayoutFixup((ushort)offset, (ushort)size, 1));
            }
            else
            {
                var temp = Layouts.GetOrAdd(fieldType, BuildLayout(fieldType));
                foreach (var fix in temp)
                {
                    layoutFixups.Add(fix with { Offset = (ushort)(fix.Offset + offset) });
                }
            }
        }

        return CompactLayout(layoutFixups);
    }

    private static LayoutFixup[] CompactLayout(List<LayoutFixup> fixups)
    {
        if (fixups.Count == 0) return [];

        fixups.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        var result = new List<LayoutFixup>(fixups.Count);
        var current = fixups[0];

        for (int i = 1; i < fixups.Count; i++)
        {
            var next = fixups[i];
            if (current.Offset + current.Size * current.Count == next.Offset && current.Size == next.Size)
            {
                current.Count += next.Count;
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);

        return result.ToArray();
    }
}
