using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers
{
    public abstract class FArchive : Stream
    {
        public UE4Version Ver;
        public EGame Game;
        public abstract string Name { get; }
        public abstract T Read<T>();
        public abstract unsafe void Read(byte* ptr, int length);
        public abstract byte[] ReadBytes(int length);
        public abstract T[] ReadArray<T>(int length);

        protected FArchive(UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
        {
            Ver = ver;
            Game = game;
        }

        public override void Flush() { }
        public override bool CanRead { get; } = true;
        public override bool CanWrite { get; } = false;
        public override void SetLength(long value) { throw new InvalidOperationException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new InvalidOperationException(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadArray<T>(int length, Func<T> getter)
        {
            var result = new T[length];

            if (length == 0)
            {
                return result;
            }

            for (int i = 0; i < length; i++)
            {
                result[i] = getter();
            }
            
            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadArray<T>(Func<T> getter)
        {
            var length = Read<int>();
            return ReadArray<T>(length, getter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadArray<T>()
        {
            var length = Read<int>();

            if (length == 0)
            {
                return new T[0];
            }

            return ReadArray<T>(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            return Read<int>() != 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadFlag()
        {
            return Read<byte>() != 0;
        }

        public string ReadFString()
        {
            // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization
            var length = Read<int>();

            if (length == 0)
            {
                return string.Empty;
            }

            // 1 byte/char is removed because of null terminator ('\0')
            if (length < 0) // LoadUCS2Char, Unicode, 16-bit, fixed-width
            {
                // If SaveNum cannot be negated due to integer overflow, Ar is corrupted.
                if (length == int.MinValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(length), "Archive is corrupted");
                }

                unsafe
                {
                    var ucs2Length = -length * sizeof(ushort);
                    var ucs2Bytes = stackalloc byte[ucs2Length];
                    Read(ucs2Bytes, ucs2Length);
#if !NO_STRING_NULL_TERMINATION_VALIDATION
                    if (ucs2Bytes[ucs2Length - 1] != 0 || ucs2Bytes[ucs2Length - 2] != 0)
                    {
                        throw new ParserException(this, "Serialized FString is not null terminated");
                    }
#endif
                    return new string((char*) ucs2Bytes);
                }
            }

            unsafe
            {
                var ansiBytes = stackalloc byte[length];
                Read(ansiBytes, length);
#if !NO_STRING_NULL_TERMINATION_VALIDATION
                if (ansiBytes[length - 1] != 0)
                {
                    throw new ParserException(this, "Serialized FString is not null terminated");
                }
#endif
                return new string((sbyte*) ansiBytes);
            }
        }
    }
}