using System;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers
{
    public class FByteArchive : FArchive
    {
        private readonly byte[] _data;

        public FByteArchive(string name, byte[] data, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : base(ver, game)
        {
            this._data = data;
            this.Name = name;
            Length = _data.Length;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count)
        {
            Buffer.BlockCopy(_data, (int) Position, buffer, offset, count);
            Position += count;
            return count;
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
        public override byte[] ReadBytes(int length)
        {
            var buffer = new byte[length];
            Read(buffer, 0, length);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Read(byte* ptr, int length)
        {
            Unsafe.CopyBlockUnaligned(ref ptr[0], ref _data[Position], (uint) length);
            Position += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length)
        {
            var size = length * Unsafe.SizeOf<T>();
            var result = new T[length];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref _data[Position], (uint) size);
            Position += size;
            return result;
        }

        public override object Clone() => new FByteArchive(Name, _data, Ver, Game) {Position = Position};
    }
}