using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers
{
    public abstract class FArchive : Stream, ICloneable
    {
        public UE4Version Ver;
        public EGame Game;
        public abstract string Name { get; }
        public abstract T Read<T>() where T : struct;
        public abstract unsafe void Serialize(byte* ptr, int length);
        public abstract byte[] ReadBytes(int length);
        public abstract T[] ReadArray<T>(int length) where T : struct;

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
        public T[] ReadArray<T>() where T : struct
        {
            var length = Read<int>();

            if (length == 0)
                return new T[0];

            return ReadArray<T>(length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(int length, Func<(TKey, TValue)> getter) where TKey : notnull
        {
            var res = new Dictionary<TKey, TValue>(length);
            for (var i = 0; i < length; i++)
            {
                var (key, value) = getter();
                res[key] = value;
            }

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(Func<(TKey, TValue)> getter) where TKey : notnull
        {
            var length = Read<int>();
            return ReadMap(length, getter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            var i = Read<int>();
            return i switch
            {
                0 => false,
                1 => true,
                _ => throw new ParserException(this, $"Invalid bool value ({i})")
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadFlag()
        {
            var i = Read<byte>();
            return i switch
            {
                0 => false,
                1 => true,
                _ => throw new ParserException(this, $"Invalid bool value ({i})")
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual unsafe void SerializeBits(void* v, long lengthBits)
        {
            Serialize((byte*) v, (int) ((lengthBits + 7) / 8));

            if (/*IsLoading &&*/ (lengthBits % 8) != 0)
            {
                ((byte*)v)[lengthBits / 8] &= (byte) ((1 << (int)(lengthBits & 7)) - 1);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read7BitEncodedInt()
        {
            int count = 0, shift = 0;
            byte b;
            do
            {
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Stream is corrupted");

                b = Read<byte>();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString()
        {
            var length = Read7BitEncodedInt();
            if (length <= 0)
                return string.Empty;
            
            unsafe
            {
                var ansiBytes = stackalloc byte[length];
                Serialize(ansiBytes, length);
                return new string((sbyte*) ansiBytes, 0, length);
            }
        }
        
        public string ReadFString()
        {
            // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization
            var length = Read<int>();

            if (length == int.MinValue)
                throw new ArgumentOutOfRangeException(nameof(length), "Archive is corrupted");

            if (length is < -65536 or > 65536)
                throw new ParserException($"Invalid FString length '{length}'");

            if (length == 0)
            {
                return string.Empty;
            }

            // 1 byte/char is removed because of null terminator ('\0')
            if (length < 0) // LoadUCS2Char, Unicode, 16-bit, fixed-width
            {
                unsafe
                {
                    length = -length;
                    var ucs2Length = length * sizeof(ushort);
                    var ucs2Bytes = stackalloc byte[ucs2Length];
                    Serialize(ucs2Bytes, ucs2Length);
#if !NO_STRING_NULL_TERMINATION_VALIDATION
                    if (ucs2Bytes[ucs2Length - 1] != 0 || ucs2Bytes[ucs2Length - 2] != 0)
                    {
                        throw new ParserException(this, "Serialized FString is not null terminated");
                    }
#endif
                    return new string((char*) ucs2Bytes, 0 , length - 1);
                }
            }

            unsafe
            {
                var ansiBytes = stackalloc byte[length];
                Serialize(ansiBytes, length);
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