using System;
using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    public readonly struct FNameEntrySerialized
    {
        public readonly string Name;
#if NAME_HASHES
        public readonly ushort NonCasePreservingHash;
        public readonly ushort CasePreservingHash;
#endif
        public FNameEntrySerialized(FArchive Ar)
        {
            Name = Ar.ReadFString();
#if NAME_HASHES
            NonCasePreservingHash = Ar.Read<ushort>();
            CasePreservingHash = Ar.Read<ushort>();
#else
            Ar.Position += 4;
#endif
        }

        public FNameEntrySerialized(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static FNameEntrySerialized[] LoadNameBatch(FArchive nameAr, int nameCount)
        {
            var result = new FNameEntrySerialized[nameCount];
            for (int i = 0; i < nameCount; i++)
            {
                result[i] = LoadNameHeader(nameAr);
            }

            return result;
        }

        public static FNameEntrySerialized[] LoadNameBatch(FArchive Ar)
        {
            var num = Ar.Read<int>();
            if (num == 0)
            {
                return Array.Empty<FNameEntrySerialized>();
            }

            Ar.Position += sizeof(uint); //var numStringBytes = Ar.Read<uint>();
            Ar.Position += sizeof(ulong); //var hashVersion = Ar.Read<ulong>();

            Ar.Position += num * sizeof(ulong); //var hashes = Ar.ReadArray<ulong>(num);
            var headers = Ar.ReadArray<FSerializedNameHeader>(num);
            var entries = new FNameEntrySerialized[num];
            for (var i = 0; i < num; i++)
            {
                var header = headers[i];
                var length = (int) header.Length;
                string s = header.IsUtf16 ? new string(Ar.ReadArray<char>(length)) : Encoding.UTF8.GetString(Ar.ReadBytes(length));
                entries[i] = new FNameEntrySerialized(s);
            }

            return entries;
        }

        private static FNameEntrySerialized LoadNameHeader(FArchive Ar)
        {
            var header = Ar.Read<FSerializedNameHeader>();

            var length = (int) header.Length;
            if (header.IsUtf16)
            {
                unsafe
                {
                    var utf16Length = length * 2;
                    var nameData = stackalloc byte[utf16Length];
                    Ar.Read(nameData, utf16Length);
                    return new FNameEntrySerialized(new string((char*) nameData, 0, length));
                }
            }

            unsafe
            {
                var nameData = stackalloc byte[length];
                Ar.Read(nameData, length);
                return new FNameEntrySerialized(new string((sbyte*) nameData, 0, length));
            }
        }

        private unsafe struct FSerializedNameHeader
        {
            private fixed byte _data[2];
            public bool IsUtf16 => (_data[0] & 0x80u) != 0;
            public uint Length => ((_data[0] & 0x7Fu) << 8) + _data[1];
        }
    }
}