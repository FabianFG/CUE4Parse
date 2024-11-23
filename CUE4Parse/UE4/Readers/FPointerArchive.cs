using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers
{
    public class FPointerArchive : FArchive
    {
        private readonly unsafe byte* _ptr;

        public unsafe FPointerArchive(string name, byte* ptr, long length, VersionContainer? versions = null) : base(versions)
        {
            _ptr = ptr;
            Name = name;
            Length = length;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count)
        {
            unsafe
            {
                int n = (int) (Length - Position);
                if (n > count) n = count;
                if (n <= 0)
                    return 0;

                if (n <= 8)
                {
                    int byteCount = n;
                    while (--byteCount >= 0)
                        buffer[offset + byteCount] = _ptr[Position + byteCount];
                }
                else
                    Unsafe.CopyBlockUnaligned(ref buffer[offset], ref _ptr[Position], (uint) n);
                Position += n;

                return n;
            }
        }

        public override int ReadAt(long position, byte[] buffer, int offset, int count)
        {
            unsafe
            {
                int n = (int) (Length - position);
                if (n > count) n = count;
                if (n <= 0)
                    return 0;

                if (n <= 8)
                {
                    int byteCount = n;
                    while (--byteCount >= 0)
                        buffer[offset + byteCount] = _ptr[position + byteCount];
                }
                else
                    Unsafe.CopyBlockUnaligned(ref buffer[offset], ref _ptr[position], (uint) n);

                return n;
            }
        }

        public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            unsafe
            {
                int n = (int) (Length - position);
                if (n > count) n = count;
                if (n <= 0)
                    return Task.FromResult(0);

                if (n <= 8)
                {
                    int byteCount = n;
                    while (--byteCount >= 0)
                        buffer[offset + byteCount] = _ptr[position + byteCount];
                }
                else
                    Unsafe.CopyBlockUnaligned(ref buffer[offset], ref _ptr[position], (uint) n);
                
                return Task.FromResult(n);
            }
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
            unsafe
            {
                var size = Unsafe.SizeOf<T>();
                var result = Unsafe.ReadUnaligned<T>(ref _ptr[Position]);
                Position += size;
                return result;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] ReadBytes(int length)
        {
            CheckReadSize(length);
            var buffer = new byte[length];
            Read(buffer, 0, length);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Serialize(byte* ptr, int length)
        {
            Unsafe.CopyBlockUnaligned(ref ptr[0], ref _ptr[Position], (uint) length);
            Position += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length)
        {
            unsafe
            {
                var size = length * Unsafe.SizeOf<T>();
                var result = new T[length];
                if (length > 0) Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref _ptr[Position], (uint) size);
                Position += size;
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReadArray<T>(T[] array)
        {
            if (array.Length == 0) return;
            unsafe
            {
                var size = array.Length * Unsafe.SizeOf<T>();
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref array[0]), ref _ptr[Position], (uint) size);
                Position += size;
            }
        }

        public override object Clone()
        {
            unsafe
            {
                return new FPointerArchive(Name, _ptr, Length, Versions) {Position = Position};
            }
        }
    }
}
