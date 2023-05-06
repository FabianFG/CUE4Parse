using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CUE4Parse.UE4.Readers;
using Ionic.Zlib;

namespace CUE4Parse.UE4.VirtualFileCache.Manifest
{
    public sealed class OptimizedContentBuildManifest
    {
        public Dictionary<string, string> HashNameMap { get; private set; }
        public TimeSpan ParseTime { get; }

        private const uint _MANIFEST_HEADER_MAGIC = 0x44BEC00Cu;

        public OptimizedContentBuildManifest(byte[] data)
        {
            var sw = Stopwatch.StartNew();
            var magic = BitConverter.ToUInt32(data, 0);
            if (magic == _MANIFEST_HEADER_MAGIC)
            {
                ParseData(data);
            }
            else
            {
                throw new NotImplementedException("JSON manifest parsing is not implemented yet");
            }

            sw.Stop();
            ParseTime = sw.Elapsed;
        }

        private void ParseData(byte[] buffer)
		{
			var reader = new FByteArchive("reader", buffer) { Position = 4 };
			var headerSize = reader.Read<int>();
			var dataSizeUncompressed = reader.Read<int>();
			var dataSizeCompressed = reader.Read<int>();
			reader.Position += 20; // SHAHash.Hash
			var storedAs = reader.Read<EManifestStorageFlags>();
            reader.Position += 4;
			reader.Seek(headerSize, SeekOrigin.Begin);

			byte[] data;
			switch (storedAs)
			{
				case EManifestStorageFlags.Compressed:
				{
					data = new byte[dataSizeUncompressed];
					var compressed = reader.ReadBytes(dataSizeCompressed);
					using var compressedStream = new MemoryStream(compressed);
					using var zlib = new ZlibStream(compressedStream, CompressionMode.Decompress);
					zlib.Read(data, 0, dataSizeUncompressed);
					break;
				}
				case EManifestStorageFlags.Encrypted:
					throw new NotImplementedException("Encrypted Manifests are not supported yet");
				default:
					data = reader.ReadBytes(dataSizeUncompressed);
					break;
			}
			reader.Dispose();

			var manifest = new FByteArchive("manifest", data);
			var startPos = (int)manifest.Position;
			var dataSize = manifest.Read<int>();
            // metadata
            manifest.Seek(startPos + dataSize, SeekOrigin.Begin);

			startPos = (int)manifest.Position;
			dataSize = manifest.Read<int>();
			// chunks
			manifest.Seek(startPos + dataSize, SeekOrigin.Begin);

            manifest.Position += 4; // dataSize
			var dataVersion = manifest.Read<EManifestMetaVersion>();
			if (dataVersion >= EManifestMetaVersion.Original)
			{
				var count = manifest.Read<int>();
                var names = new string[count];
                HashNameMap = new Dictionary<string, string>(count);

				for (var i = 0; i < count; i++) // Filename
				{
                    names[i] = manifest.ReadFString();
				}

				for (var i = 0; i < count; i++) // SymlinkTarget
				{
                    int length = manifest.Read<int>();
                    manifest.Seek(length < 0 ? -length * 2 : length, SeekOrigin.Current);
				}

				var shaOffset = (int)manifest.Position;
				for (var i = 0; i < count; i++) // FileHash
				{
					var hash = BitConverter.ToString(data, shaOffset + i * 20, 20).Replace("-", "");
                    HashNameMap[hash] = names[i];
                }
			}
		}
    }
}
