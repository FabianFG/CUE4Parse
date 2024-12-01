using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers
{
    public class FByteArchive : FArchive
    {
        private readonly byte[] _data;

        public FByteArchive(string name, byte[] data, VersionContainer? versions = null) : base(versions)
        {
            _data = data;
            Name = name;
            Length = _data.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = (int) (Length - Position);
            if (n > count) n = count;
            if (n <= 0)
                return 0;

            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _data[Position + byteCount];
            }
            else
                Buffer.BlockCopy(_data, (int) Position, buffer, offset, n);
            Position += n;

            return n;
        }

        public override int ReadAt(long position, byte[] buffer, int offset, int count)
        {
            int n = (int) (Length - position);
            if (n > count) n = count;
            if (n <= 0)
                return 0;

            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _data[position + byteCount];
            }
            else
                Buffer.BlockCopy(_data, (int) position, buffer, offset, n);

            return n;
        }

        public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReadAt(position, buffer, offset, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentOutOfRangeException()
            };
            return Position;
        }

        public override bool CanSeek { get; } = true;
        public override long Length { get; }
        public override long Position { get; set; }
        public override string Name { get; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T Read<T>()
        {
            var size = Unsafe.SizeOf<T>();
            var result = Unsafe.ReadUnaligned<T>(ref _data[Position]);
            Position += size;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Serialize(byte* ptr, int length)
        {
            Unsafe.CopyBlockUnaligned(ref ptr[0], ref _data[Position], (uint) length);
            Position += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length)
        {
            var size = length * Unsafe.SizeOf<T>();
            CheckReadSize(size);
            var result = new T[length];
            if (length > 0) Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref _data[Position], (uint) size);
            Position += size;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReadArray<T>(T[] array)
        {
            if (array.Length == 0) return;
            var size = array.Length * Unsafe.SizeOf<T>();
            CheckReadSize(size);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref array[0]), ref _data[Position], (uint) size);
            Position += size;
        }

        public override object Clone() => new FByteArchive(Name, _data, Versions) {Position = Position};
    }
}
