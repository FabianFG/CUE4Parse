using System.Collections.Generic;
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

        private static FNameEntrySerialized LoadNameHeader(FArchive nameAr)
        {
            var header = new FSerializeNameHeader(nameAr);

            var length = (int) header.Length;
            if (header.IsUtf16)
            {
                unsafe
                {
                    var utf16Length = length * 2;
                    var nameData = stackalloc byte[utf16Length];
                    nameAr.Read(nameData, utf16Length);
                    return new FNameEntrySerialized(new string((char*)nameData, 0, length));
                }
            }
            else
            {
                unsafe
                {
                    var nameData = stackalloc byte[length];
                    nameAr.Read(nameData, length);
                    return new FNameEntrySerialized(new string((sbyte*) nameData, 0, length));
                }
            }
        }

        private readonly struct FSerializeNameHeader
        {
            public readonly bool IsUtf16;
            public readonly uint Length;
            public FSerializeNameHeader(FArchive Ar)
            {
                unsafe
                {
                    var data = stackalloc byte[2];
                    Ar.Read(data, 2);
                    IsUtf16 = (data[0] & 0x80u) != 0;
                    Length = ((data[0] & 0x7Fu) << 8) + data[1];
                }
            }
        }
    }
}