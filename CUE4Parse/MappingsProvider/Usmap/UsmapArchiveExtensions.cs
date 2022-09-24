using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap
{
    public static class UsmapArchiveExtensions
    {
        private const int InvalidNameIndex = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? ReadName(this FArchive Ar, IReadOnlyList<string> nameLut)
        {
            var idx = Ar.ReadNameEntry();
            return idx != InvalidNameIndex ? nameLut[idx] : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadNameEntry(this FArchive Ar)
        {
            return Ar.Read<int>();
        }

        public static unsafe string ReadStringUnsafe(this FArchive Ar, int nameLength)
        {
            var nameBytes = stackalloc byte[nameLength];
            Ar.Serialize(nameBytes, nameLength);
            return new string((sbyte*) nameBytes, 0, nameLength);
        }
    }
}