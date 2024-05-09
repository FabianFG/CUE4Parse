using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.AssetRegistry.Readers
{
    public abstract class FAssetRegistryArchive : FArchive
    {
        protected readonly FArchive baseArchive;
        public FAssetRegistryHeader Header;
        public FNameEntrySerialized[] NameMap;

        public abstract void SerializeTagsAndBundles(FAssetData assetData);

        public FAssetRegistryArchive(FArchive Ar, FAssetRegistryHeader header) : base(Ar.Versions)
        {
            baseArchive = Ar;
            Header = header;
            NameMap = [];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReadArray<T>(T[] array)
            => baseArchive.ReadArray(array);
    }
}
