using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FIoStoreEntry : VfsEntry
    {
        public override bool IsEncrypted => IoStoreReader.IsEncrypted;
        public override CompressionMethod CompressionMethod
        {
            get
            {
                var tocResource = IoStoreReader.TocResource;
                var firstBlockIndex = (int) (Offset / tocResource.Header.CompressionBlockSize);
                return tocResource.CompressionMethods[tocResource.CompressionBlocks[firstBlockIndex].CompressionMethodIndex];
            }
        }

        private readonly uint _tocEntryIndex;
        public FIoChunkId ChunkId => IoStoreReader.TocResource.ChunkIds[_tocEntryIndex];
        public bool IsPackageData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ChunkId.ChunkType == (IoStoreReader.Game >= GAME_UE5_0
                ? (byte) EIoChunkType5.ExportBundleData
                : (byte) EIoChunkType.ExportBundleData);
        }
        public bool IsOptionalPackage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsOptionalPackagePath(Path);
        }

        public bool IsOptionalSegmentPackage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsPackageData && ChunkId._chunkIndex != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOptionalPackagePath(string path)
        {
            var extensionSeparator = path.LastIndexOf('.');
            if (extensionSeparator <= 1 || !path.AsSpan(..extensionSeparator).EndsWith(".o", StringComparison.Ordinal))
                return false;

            var extension = path[(extensionSeparator + 1)..];
            return UePackageExtensionsSet.Contains(extension) || UePackagePayloadExtensionsSet.Contains(extension);
        }

        /// <inheritdoc />
        public override bool IsHidden
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public FIoStoreEntry(IoStoreReader reader, string path, uint tocEntryIndex) : base(reader, path)
        {
            _tocEntryIndex = tocEntryIndex;
            IsHidden = IsOptionalPackagePath(path);
            ref var offsetLength = ref reader.TocResource.ChunkOffsetLengths[tocEntryIndex];
            Offset = (long) offsetLength.Offset;
            Size = (long) offsetLength.Length;
        }

        public FIoStoreEntry(IoStoreReader reader, uint tocEntryIndex) : base(reader, "NonIndexed/")
        {
            _tocEntryIndex = tocEntryIndex;
            Path += $"0x{ChunkId.ChunkId:X8}.{ChunkId.GetExtension(reader)}";

            IsHidden = IsOptionalPackagePath(Path);

            ref var offsetLength = ref reader.TocResource.ChunkOffsetLengths[tocEntryIndex];
            Offset = (long) offsetLength.Offset;
            Size = (long) offsetLength.Length;
        }

        public IoStoreReader IoStoreReader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (IoStoreReader) Vfs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read(FByteBulkDataHeader? header = null) => Vfs.Extract(this, header);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader(FByteBulkDataHeader? header = null) => new FByteArchive(Path, Read(header), Vfs.Versions);
    }
}
