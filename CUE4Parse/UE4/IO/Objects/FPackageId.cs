using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static CUE4Parse.Utils.CityHash;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPackageId : IEquatable<FPackageId>
    {
        public readonly ulong id;

        public FPackageId(ulong id)
        {
            this.id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FPackageId other) => id == other.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is FPackageId other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => id.GetHashCode();

        public override string ToString() => id.ToString();

        public static FPackageId FromName(string name)
        {
            var nameStr = name.ToLowerInvariant();
            var nameBuf = Encoding.Unicode.GetBytes(nameStr);
            var hash = CityHash64(nameBuf);
            Trace.Assert(hash != ~0uL, $"Package name hash collision \"{nameStr}\" and InvalidId");
            return new FPackageId(hash);
        }
    }
}
