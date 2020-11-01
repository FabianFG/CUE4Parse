using System;
using System.Diagnostics;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.UE4.IO.Objects
{
    
    public enum EIoStoreTocReadOptions
    {
        Default,
        ReadDirectoryIndex	= (1 << 0),
        ReadTocMeta			= (1 << 1),
        ReadAll				= ReadDirectoryIndex | ReadTocMeta
    } 
    
    public class FIoStoreTocResource
    {
        public readonly FIoStoreTocHeader Header;
        public readonly FIoChunkId[] ChunkIds;
        public readonly FIoOffsetAndLength[] ChunkOffsetLengths;
        public readonly FIoStoreTocCompressedBlockEntry[] CompressionBlocks;
        public readonly CompressionMethod[] CompressionMethods;

        public readonly byte[]? DirectoryIndexBuffer;
        public readonly FIoStoreTocEntryMeta[]? ChunkMetas;

        public FIoStoreTocResource(FArchive Ar, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.Default)
        {
            Header = new FIoStoreTocHeader(Ar);
            ChunkIds = Ar.ReadArray<FIoChunkId>((int) Header.TocEntryCount);
            
            ChunkOffsetLengths = new FIoOffsetAndLength[Header.TocEntryCount];
            for (int i = 0; i < Header.TocEntryCount; i++)
            {
                ChunkOffsetLengths[i] = new FIoOffsetAndLength(Ar);
            }
            
            CompressionBlocks = new FIoStoreTocCompressedBlockEntry[Header.TocCompressedBlockEntryCount];
            for (int i = 0; i < Header.TocCompressedBlockEntryCount; i++)
            {
                CompressionBlocks[i] = new FIoStoreTocCompressedBlockEntry(Ar);
            }
            
            unsafe
            {
                var bufferSize = (int) (Header.CompressionMethodNameLength * Header.CompressionMethodNameCount);
                var buffer = stackalloc byte[bufferSize];
                Ar.Read(buffer, bufferSize);
                CompressionMethods = new CompressionMethod[Header.CompressionMethodNameCount + 1];
                CompressionMethods[0] = CompressionMethod.None;
                for (var i = 0; i < Header.CompressionMethodNameCount; i++)
                {
                    var name = new string((sbyte*) buffer + i * Header.CompressionMethodNameLength, 0, (int) Header.CompressionMethodNameLength).TrimEnd('\0');
                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (!Enum.TryParse(name, out CompressionMethod method))
                    {
                        Log.Warning($"Unknown compression method '{name}' in {Ar.Name}");
                        method = CompressionMethod.Unknown;
                    }
                    CompressionMethods[i + 1] = method;
                }
            }
            
            // Chunk block signatures
            if (Header.ContainerFlags.HasFlag(EIoContainerFlags.Signed))
            {
                var hashSize = Ar.Read<int>();
                // tocSignature and blockSignature both byte[hashSize]
                // and ChunkBlockSignature of FSHAHash[Header.TocCompressedBlockEntryCount]
                Ar.Position += hashSize + hashSize + FSHAHash.SIZE * Header.TocCompressedBlockEntryCount;
                
                // You could verify hashes here but nah
            }

            // Directory index
            if (Header.Version >= EIoStoreTocVersion.DirectoryIndex && 
                readOptions.HasFlag(EIoStoreTocReadOptions.ReadDirectoryIndex) &&
                Header.ContainerFlags.HasFlag(EIoContainerFlags.Indexed) &&
                Header.DirectoryIndexSize > 0)
            {
                DirectoryIndexBuffer = Ar.ReadBytes((int) Header.DirectoryIndexSize);
            }
            
            // Meta
            if (readOptions.HasFlag(EIoStoreTocReadOptions.ReadTocMeta))
            {
                ChunkMetas = new FIoStoreTocEntryMeta[Header.TocEntryCount];
                for (int i = 0; i < Header.TocEntryCount; i++)
                {
                    ChunkMetas[i] = new FIoStoreTocEntryMeta(Ar);
                }
            }
        }
    }
}