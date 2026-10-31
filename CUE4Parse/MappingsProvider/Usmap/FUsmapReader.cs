using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap;

public sealed class FUsmapReader : FArchive
{
    private readonly FArchive Ar;
    public readonly EUsmapVersion Version;
    private readonly EPropertyFlags[] _flagLut = [];
    private readonly string[] _nameLut;

    public FUsmapReader(FArchive ar, EUsmapVersion version) : base(ar.Versions)
    {
        Ar = ar;
        Version = version;
        var nameSize = Read<int>();
        _nameLut = new string[nameSize];
        for (var i = 0; i < nameSize; i++)
        {
            var nameLength = Version >= EUsmapVersion.LongFName ? Read<ushort>() : Read<byte>();
            _nameLut[i] = Encoding.UTF8.GetString(ReadSpan(nameLength));
        }

        if (Version >= EUsmapVersion.ExtendedMetadata)
        {
            _flagLut = Ar.ReadArray<EPropertyFlags>();
        }
    }

    // Clones share the tables that were already read instead of reading them again at the current position
    private FUsmapReader(FArchive ar, EUsmapVersion version, string[] nameLut, EPropertyFlags[] flagLut) : base(ar.Versions)
    {
        Ar = ar;
        Version = version;
        _nameLut = nameLut;
        _flagLut = flagLut;
    }

    private const int InvalidNameIndex = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ReadName()
    {
        var idx = Read<int>();
        return idx != InvalidNameIndex ? _nameLut[idx] : null;
    }

    public EPropertyFlags ReadPropertyFlags(string propertyName)
    {
        if (Version < EUsmapVersion.ExtendedMetadata)
            return EPropertyFlags.None;

        var flagIndex = Read<ushort>();
        // A bad index means the reader is out of step with the file, so everything after it would be garbage
        if (flagIndex >= _flagLut.Length)
            throw new ParserException(this, $"Usmap property '{propertyName}' has invalid flag index {flagIndex} (table size {_flagLut.Length})");
        return _flagLut[flagIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count) => Ar.Read(buffer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin) => Ar.Seek(offset, origin);

    public override bool CanSeek => Ar.CanSeek;
    public override long Length => Ar.Length;

    public override long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ar.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Ar.Position = value;
    }

    public override string Name => Ar.Name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Read<T>() => Ar.Read<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length) => Ar.ReadBytes(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<byte> ReadSpan(int length) => Ar.ReadSpan(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe void Serialize(byte* ptr, int length) => Ar.Serialize(ptr, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T[] ReadArray<T>(int length) => Ar.ReadArray<T>(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void ReadArray<T>(T[] array) => Ar.ReadArray(array);

    public override object Clone() => new FUsmapReader((FArchive) Ar.Clone(), Version, _nameLut, _flagLut);
}