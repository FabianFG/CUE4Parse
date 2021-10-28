using System;
using System.Diagnostics;
using System.IO;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.Compression.Compression;
using static CUE4Parse.UE4.Objects.Core.Misc.ECompressionFlags;

namespace CUE4Parse.UE4.Readers
{
    public class FArchiveLoadCompressedProxy : FArchive
    {
        private readonly byte[] _compressedData;
        private int _currentIndex;
        private readonly byte[] _tmpData;
        private int _tmpDataPos;
        private int _tmpDataSize;
        private bool _shouldSerializeFromArray;
        private long _rawBytesSerialized;
        private readonly string _compressionFormat;
        private readonly ECompressionFlags _compressionFlags;

        public FArchiveLoadCompressedProxy(string name, byte[] compressedData, string compressionFormat, ECompressionFlags flags = COMPRESS_None, VersionContainer? versions = null) : base(versions)
        {
            Name = name;
            _compressedData = compressedData;
            _compressionFormat = compressionFormat;
            _compressionFlags = flags;

            _tmpData = new byte[LOADING_COMPRESSION_CHUNK_SIZE];
            _tmpDataPos = LOADING_COMPRESSION_CHUNK_SIZE;
            _tmpDataSize = LOADING_COMPRESSION_CHUNK_SIZE;
        }

        public override string Name { get; }

        public override object Clone() => new FArchiveLoadCompressedProxy(Name, _compressedData, _compressionFormat, _compressionFlags, Versions);

        public override int Read(byte[]? dstData, int offset, int count)
        {
            if (_shouldSerializeFromArray)
            {
                // SerializedCompressed reads the compressed data from here
                Trace.Assert(_currentIndex + count <= _compressedData.Length);
                Buffer.BlockCopy(_compressedData, _currentIndex, dstData!, 0, count);
                _currentIndex += count;
                return count;
            }
            // Regular call to serialize, read from temp buffer
            else
            {
                var dstPos = 0;
                while (count > 0)
                {
                    var bytesToCopy = Math.Min(count, _tmpDataSize - _tmpDataPos);
                    // Enough room in buffer to copy some data.
                    if (bytesToCopy > 0)
                    {
                        // We pass in a NULL pointer when forward seeking. In that case we don't want
                        // to copy the data but only care about pointing to the proper spot.
                        if (dstData != null)
                        {
                            Buffer.BlockCopy(_tmpData, _tmpDataPos, dstData, dstPos, bytesToCopy);
                            dstPos += bytesToCopy;
                        }
                        count -= bytesToCopy;
                        _tmpDataPos += bytesToCopy;
                        _rawBytesSerialized += bytesToCopy;
                    }
                    // Tmp buffer fully exhausted, decompress new one.
                    else
                    {
                        // Decompress more data. This will call Serialize again so we need to handle recursion.
                        DecompressMoreData();

                        if (_tmpDataSize == 0)
                        {
                            // wanted more but couldn't get any
                            // avoid infinite loop
                            throw new ParserException();
                        }
                    }
                }

                return dstPos;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Trace.Assert(origin == SeekOrigin.Begin);
            var currentPos = Position;
            var difference = offset - currentPos;
            // We only support forward seeking.
            Trace.Assert(difference >= 0);
            // Seek by serializing data, albeit with NULL destination so it's just decompressing data.
            Read(null, 0, (int) difference);
            return Position;
        }

        public override bool CanSeek => true;
        public override long Length => throw new InvalidOperationException();
        public override long Position
        {
            get => _rawBytesSerialized;
            set => Seek(value, SeekOrigin.Begin);
        }

        private void DecompressMoreData()
        {
            // This will call Serialize so we need to indicate that we want to serialize from array.
            _shouldSerializeFromArray = true;
            SerializeCompressedNew(_tmpData, LOADING_COMPRESSION_CHUNK_SIZE, _compressionFormat, _compressionFlags, false, out var decompressedLength);
            // last chunk will be partial :
            //	all chunks before last should have size == LOADING_COMPRESSION_CHUNK_SIZE
            Trace.Assert(decompressedLength <= LOADING_COMPRESSION_CHUNK_SIZE);
            _shouldSerializeFromArray = false;
            // Buffer is filled again, reset.
            _tmpDataPos = 0;
            _tmpDataSize = (int) decompressedLength;
        }
    }
}