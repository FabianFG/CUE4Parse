using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Readers
{
    public struct FFrozenMemoryImagePtr
    {
        private const ulong TypeIndexMask = ((1UL << 23) - 1UL) << 1;

        public readonly ulong _packed;
        public readonly bool IsFrozen;
        public readonly long OffsetFromThis;
        public readonly int TypeIndex = -1;

        public FFrozenMemoryImagePtr(FMemoryImageArchive Ar)
        {
            _packed = Ar.Read<ulong>();
            IsFrozen = (_packed & 1) != 0;
            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                OffsetFromThis = (long)_packed >> 24;
                TypeIndex = (int) ((_packed & TypeIndexMask) >> 1) - 1;
            }
            else
            {
                OffsetFromThis = (long)_packed >> 1;
            }
        }

    }

    public class FMemoryImageArchive : FArchive
    {
        protected readonly FArchive InnerArchive;
        public IReadOnlyDictionary<int, FName>? Names;

        public FMemoryImageArchive(FArchive Ar) : base(Ar.Versions)
        {
            InnerArchive = Ar;
        }

        public override int Read(byte[] buffer, int offset, int count)
            => InnerArchive.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => InnerArchive.Seek(offset, origin);

        public override bool CanSeek => InnerArchive.CanSeek;
        public override long Length => InnerArchive.Length;

        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InnerArchive.Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => InnerArchive.Position = value;
        }

        public override string Name => InnerArchive.Name;

        public override T Read<T>()
            => InnerArchive.Read<T>();

        public override byte[] ReadBytes(int length)
            => InnerArchive.ReadBytes(length);

        public override unsafe void Serialize(byte* ptr, int length)
            => InnerArchive.Serialize(ptr, length);

        public override T[] ReadArray<T>(int length)
            => InnerArchive.ReadArray<T>(length);

        public override void ReadArray<T>(T[] array)
            => InnerArchive.ReadArray(array);

        public override T[] ReadArray<T>()
        {
            var initialPos = Position;
            var dataPtr = new FFrozenMemoryImagePtr(this);
            var arrayNum = Read<int>();
            var arrayMax = Read<int>();
            if (arrayNum != arrayMax)
            {
                throw new ParserException(this, $"Num ({arrayNum}) != Max ({arrayMax})");
            }
            if (arrayNum == 0)
            {
                return Array.Empty<T>();
            }

            var continuePos = Position;
            Position = initialPos + dataPtr.OffsetFromThis;
            var data = InnerArchive.ReadArray<T>(arrayNum);
            Position = continuePos;
            return data;
        }

        public override T[] ReadArray<T>(Func<T> getter)
        {
            var initialPos = Position;
            var dataPtr = new FFrozenMemoryImagePtr(this);
            var arrayNum = Read<int>();
            var arrayMax = Read<int>();
            if (arrayNum != arrayMax)
            {
                throw new ParserException(this, $"Num ({arrayNum}) != Max ({arrayMax})");
            }
            if (arrayNum == 0)
            {
                return Array.Empty<T>();
            }

            var continuePos = Position;
            Position = initialPos + dataPtr.OffsetFromThis;
            var data = new T[arrayNum];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = getter();
                Position = Position.Align(8);
            }
            Position = continuePos;
            return data;
        }

        public T[] ReadArrayOfPtrs<T>(Func<T> getter)
        {
            var initialPos = Position;
            var dataPtr = new FFrozenMemoryImagePtr(this);
            var arrayNum = Read<int>();
            var arrayMax = Read<int>();
            if (arrayNum != arrayMax)
            {
                throw new ParserException(this, $"Num ({arrayNum}) != Max ({arrayMax})");
            }
            if (arrayNum == 0)
            {
                return Array.Empty<T>();
            }

            var continuePos = Position;
            Position = initialPos + dataPtr.OffsetFromThis;
            var data = new T[arrayNum];
            for (int i = 0; i < data.Length; i++)
            {
                var entryPtrPos = Position;
                var entryPtr = new FFrozenMemoryImagePtr(this);
                Position = entryPtrPos + entryPtr.OffsetFromThis;
                data[i] = getter();
                Position = (entryPtrPos + 8).Align(8);
            }
            Position = continuePos;
            return data;
        }

        public int[] ReadHashTable()
        {
            var initialPos = Position;
            var hashPtr = new FFrozenMemoryImagePtr(this);
            //var nextIndexPtr = Read<FFrozenMemoryImagePtr>();
            var nextIndexPtr = new FFrozenMemoryImagePtr(this);
            var hashMask = Read<uint>();
            var indexSize = Read<uint>();

            return Array.Empty<int>(); // TODO always empty array for now
            //if (indexSize == 0) return Array.Empty<int>();

            //var t1 = Read<uint>();
            //return Array.Empty<int>();
        }

        public override string ReadFString()
        {
            var initialPos = Position;
            var dataPtr = new FFrozenMemoryImagePtr(this);
            var arrayNum = Read<int>();
            var arrayMax = Read<int>();
            if (arrayNum != arrayMax)
            {
                throw new ParserException(this, $"Num ({arrayNum}) != Max ({arrayMax})");
            }
            if (arrayNum <= 1)
            {
                return string.Empty;
            }

            var continuePos = Position;
            Position = initialPos + dataPtr.OffsetFromThis;
            var ucs2Bytes = ReadBytes(arrayNum * 2);
            Position = continuePos;
#if !NO_STRING_NULL_TERMINATION_VALIDATION
            if (ucs2Bytes[^1] != 0 || ucs2Bytes[^2] != 0)
            {
                throw new ParserException(this, "Serialized FString is not null terminated");
            }
#endif
            return Encoding.Unicode.GetString(ucs2Bytes, 0, ucs2Bytes.Length - 2);
        }

        public override object Clone() => new FMemoryImageArchive((FArchive) InnerArchive.Clone());

        public IEnumerable<(KeyType, ValueType)> ReadTMap<KeyType, ValueType>(Func<KeyType> keyGetter, Func<ValueType> valueGetter, int keyStructSize, int valueStructSize) where KeyType : notnull
        {
            var pairs = ReadTSet(() => (keyGetter(), valueGetter()), (keyStructSize + valueStructSize).Align(8));
            return pairs;
        }

        public IEnumerable<ElementType> ReadTSet<ElementType>(Func<ElementType> elementGetter, int elementStructSize)
        {
            var elements = ReadTSparseArray(elementGetter, elementStructSize + 4 + 4); // Add HashNextId and HashIndex from TSetElement
            Position += 8 + 4; // skip Hash and HashSize
            return elements;
        }

        public IEnumerable<ElementType> ReadTSparseArray<ElementType>(Func<ElementType> elementGetter, int elementStructSize)
        {
            var initialPos = Position;
            var dataPtr = new FFrozenMemoryImagePtr(this);
            var dataNum = Read<int>();
            var dataMax = Read<int>();
            var allocationFlags = ReadTBitArray();
            Position += 4 + 4; // skip FirstFreeIndex and NumFreeIndices
            if (dataNum == 0)
            {
                return new List<ElementType>();
            }

            var continuePos = Position;
            Position = initialPos + dataPtr.OffsetFromThis;
            var data = new List<ElementType>(dataNum);
            for (var i = 0; i < dataNum; ++i)
            {
                var start = Position;
                if (allocationFlags[i])
                {
                    data.Add(elementGetter());
                }
                Position = start + elementStructSize;
            }

            Position = continuePos;
            return data;
        }

        public BitArray ReadTBitArray()
        {
            var initialPos = Position;
            var dataPtr = new FFrozenMemoryImagePtr(this);
            var numBits = Read<int>();
            var maxBits = Read<int>();
            if (numBits == 0)
            {
                return new BitArray(0);
            }

            var continuePos = Position;
            Position = initialPos + dataPtr.OffsetFromThis;
            var data = InnerArchive.ReadArray<int>(numBits.DivideAndRoundUp(32));
            Position = continuePos;
            return new BitArray(data) { Length = numBits };
        }

        public override FName ReadFName()
        {
            Position += 4 + 4 + 4;
            if (Names != null && Names.TryGetValue((int) Position, out var name))
            {
                return name;
            }
            return default;
        }
    }
}
