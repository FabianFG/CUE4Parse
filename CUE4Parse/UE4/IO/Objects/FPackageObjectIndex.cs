using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPackageObjectIndex : IEquatable<FPackageObjectIndex>
    {
        public const int Size = sizeof(ulong);
        public const int IndexBits = 62;
        public const ulong IndexMask = (1UL << IndexBits) - 1UL;
        public const ulong TypeMask = ~IndexMask;
        public const int TypeShift = IndexBits;
        public const ulong Invalid = ~0UL;

        public readonly ulong TypeAndId;
        public EType Type => (EType) (TypeAndId >> TypeShift);
        public ulong Value => TypeAndId & IndexMask;

        public bool IsNull => TypeAndId == Invalid;
        public bool IsExport => Type == EType.Export;
        public bool IsImport => IsScriptImport || IsPackageImport;
        public bool IsScriptImport => Type == EType.ScriptImport;
        public bool IsPackageImport => Type == EType.PackageImport;
        public uint AsExport => (uint) TypeAndId;

        public FPackageImportReference AsPackageImportRef => new()
        {
            ImportedPackageIndex = (uint) ((TypeAndId & IndexMask) >> 32),
            ImportedPublicExportHashIndex = (uint) TypeAndId
        };

        public FPackageObjectIndex(ulong typeAndId)
        {
            TypeAndId = typeAndId;
        }

        public bool Equals(FPackageObjectIndex other)
        {
            return TypeAndId == other.TypeAndId;
        }

        public override bool Equals(object? obj)
        {
            return obj is FPackageObjectIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return TypeAndId.GetHashCode();
        }

        public static bool operator ==(FPackageObjectIndex left, FPackageObjectIndex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FPackageObjectIndex left, FPackageObjectIndex right)
        {
            return !left.Equals(right);
        }
    }

    public enum EType
    {
        Export,
        ScriptImport,
        PackageImport,
        Null,
        TypeCount = Null
    }
}
