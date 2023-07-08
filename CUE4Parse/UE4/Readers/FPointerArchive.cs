using System;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers;

public unsafe class FPointerArchive(string name, byte* ptr, long length, VersionContainer? versions = null) : FArchive(versions)
{
    private readonly byte* _ptr = ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        int n = (int) (Length - Position);
        if (n > count) n = count;
        if (n <= 0) return 0;

        if (n <= 8)
        {
            int byteCount = n;
            while (--byteCount >= 0)
                buffer[offset + byteCount] = _ptr[Position + byteCount];
        }
        else Unsafe.CopyBlockUnaligned(ref buffer[offset], ref _ptr[Position], (uint) n);

        Position += n;

        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(offset))
        };
        return Position;
    }

    public override bool CanSeek => true;
    public override long Length { get; } = length;
    public override long Position { get; set; }
    public override string Name { get; } = name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Read<T>()
    {
        var size = Unsafe.SizeOf<T>();
        var result = Unsafe.ReadUnaligned<T>(ref _ptr[Position]);
        Position += size;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length)
    {
        var buffer = new byte[length];
        Read(buffer, 0, length);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(byte* ptr, int length)
    {
        Unsafe.CopyBlockUnaligned(ref ptr[0], ref _ptr[Position], (uint) length);
        Position += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T[] ReadArray<T>(int length)
    {
        var size = length * Unsafe.SizeOf<T>();
        var result = new T[length];
        if (length > 0) Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref _ptr[Position], (uint) size);
        Position += size;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void ReadArray<T>(T[] array)
    {
        if (array.Length == 0) return;
        var size = array.Length * Unsafe.SizeOf<T>();
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref array[0]), ref _ptr[Position], (uint) size);
        Position += size;
    }

    public override object Clone()
    {
        return new FPointerArchive(Name, _ptr, Length, Versions) { Position = Position };
    }
}
