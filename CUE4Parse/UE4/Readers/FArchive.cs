using System;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers
{
    public abstract class FArchive : Stream, ICloneable
    {
        public UE4Version Ver;
        public EGame Game;
        public abstract string Name { get; }
        public abstract T Read<T>();
        public abstract unsafe void Read(byte* ptr, int length);
        public abstract byte[] ReadBytes(int length);
        public abstract T[] ReadArray<T>(int length);

        protected FArchive(EGame game = EGame.GAME_UE4_LATEST, UE4Version ver = UE4Version.VER_UE4_DETERMINE_BY_GAME)
        {
            Game = game;
            Ver = ver == UE4Version.VER_UE4_DETERMINE_BY_GAME ? game.GetVersion() : ver;
        }

        public override void Flush() { }
        public override bool CanRead { get; } = true;
        public override bool CanWrite { get; } = false;
        public override void SetLength(long value) { throw new InvalidOperationException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new InvalidOperationException(); }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadArray<T>(T[] array, Func<T> getter)
        {
            // array is a reference type
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = getter();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadArray<T>(int length, Func<T> getter)
        {
            var result = new T[length];

            if (length == 0)
            {
                return result;
            }

            ReadArray(result, getter);
            
            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadArray<T>(Func<T> getter)
        {
            var length = Read<int>();
            return ReadArray(length, getter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ReadArray<T>()
        {
            var length = Read<int>();

            if (length == 0)
                return new T[0];

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

            if (length > 512 || length < -512)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Archive is corrupted");
            }
            if (length == 0)
            {
                return string.Empty;
            }

            // 1 byte/char is removed because of null terminator ('\0')
            if (length < 0) // LoadUCS2Char, Unicode, 16-bit, fixed-width
            {
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
                    return new string((char*) ucs2Bytes, 0 , -length - 1);
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
                return new string((sbyte*) ansiBytes, 0, length - 1);
            }
        }

        public abstract object Clone();
    }
}