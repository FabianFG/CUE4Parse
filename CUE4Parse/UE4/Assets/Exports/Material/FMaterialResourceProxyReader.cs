using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class FMaterialResourceProxyReader : FArchive
    {
        protected readonly FArchive baseArchive;
        public FNameEntrySerialized[] NameMap;
        private long _offsetToFirstResource;

        public FMaterialResourceProxyReader(FArchive Ar) : base(Ar.Versions)
        {
            baseArchive = Ar;
            NameMap = Ar.ReadArray(() => new FNameEntrySerialized(Ar));
            var locs = Ar.ReadArray<FMaterialResourceLocOnDisk>();
            //check(locs[0].offset == 0)
            var numBytes = Ar.Read<int>();

            _offsetToFirstResource = Ar.Position;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FMaterialResourceLocOnDisk
        {
            /** Relative offset to package (uasset/umap + uexp) beginning */
            public uint Offset;
            public ERHIFeatureLevel FeatureLevel;
            public EMaterialQualityLevel QualityLevel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FName ReadFName()
        {
            var nameIndex = baseArchive.Read<int>();
            var number = baseArchive.Read<int>();
#if !NO_FNAME_VALIDATION
            if (nameIndex < 0 || nameIndex >= NameMap.Length)
            {
                throw new ParserException(baseArchive, $"FName could not be read, requested index {nameIndex}, name map size {NameMap.Length}");
            }
#endif
            return new FName(NameMap[nameIndex], nameIndex, number);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count)
            => baseArchive.Read(buffer, offset, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin)
            => baseArchive.Seek(offset, origin);

        public override bool CanSeek => baseArchive.CanSeek;
        public override long Length => baseArchive.Length;
        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => baseArchive.Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => baseArchive.Position = value;
        }

        public override string Name => baseArchive.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T Read<T>()
            => baseArchive.Read<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] ReadBytes(int length)
            => baseArchive.ReadBytes(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Serialize(byte* ptr, int length)
            => baseArchive.Serialize(ptr, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length)
            => baseArchive.ReadArray<T>(length);

        public override object Clone() => new FMaterialResourceProxyReader((FArchive) baseArchive.Clone());
    }
}