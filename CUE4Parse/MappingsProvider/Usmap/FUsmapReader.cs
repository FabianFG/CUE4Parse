using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap;

public sealed class FUsmapReader : FArchive
{
    private readonly FArchive Ar;
    public readonly EUsmapVersion Version;
    private EPropertyFlags[] _flagLut = [];
    private string[] _nameLut;

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
        if (flagIndex >= _flagLut.Length)
        {
            Log.Error("Usmap property '{PropertyName}' has invalid flag index {FlagIndex} (table size {FlagLutLength})", propertyName, flagIndex, _flagLut.Length);
            return EPropertyFlags.None;
        }
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

    public override object Clone()
    {
        var clone = new FUsmapReader((FArchive) Ar.Clone(), Version);
        clone._nameLut = _nameLut;
        clone._flagLut = _flagLut;
        return clone;
    }
}