using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap;

public class FUsmapReader(FArchive Ar, EUsmapVersion version) : FArchive(Ar.Versions)
{
    public readonly EUsmapVersion Version = version;
    public EPropertyFlags[] FlagLUT = [];

    public EPropertyFlags ReadPropertyFlags(string propertyName)
    {
        if (Version < EUsmapVersion.PropertyFlags)
            return EPropertyFlags.None;

        var flagIndex = FlagLUT.Length <= byte.MaxValue ? Read<byte>() : Read<ushort>();
        if (flagIndex >= FlagLUT.Length)
            throw new ParserException(this, $"Usmap property '{propertyName}' has invalid flag index {flagIndex} (table size {FlagLUT.Length})");
        return FlagLUT[flagIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
        => Ar.Read(buffer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
        => Ar.Seek(offset, origin);

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
    public override T Read<T>()
        => Ar.Read<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length)
        => Ar.ReadBytes(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe void Serialize(byte* ptr, int length)
        => Ar.Serialize(ptr, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T[] ReadArray<T>(int length)
        => Ar.ReadArray<T>(length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void ReadArray<T>(T[] array)
        => Ar.ReadArray(array);

    public override object Clone()
    {
        var clone = new FUsmapReader((FArchive) Ar.Clone(), Version);
        clone.FlagLUT = FlagLUT;
        return clone;
    }
}
