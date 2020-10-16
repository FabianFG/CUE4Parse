using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Pak
{
    public partial class PakFileReader
    {

        private static readonly ILogger log = Log.ForContext<PakFileReader>();

        public readonly FArchive Ar;
        public bool IsConcurrent { get; set; } = false;
        public readonly string FileName; 
        public readonly FPakInfo Info;

        public string MountPoint { get; private set; }
        public IReadOnlyDictionary<string, GameFile> Files { get; private set; }
        public int FileCount { get; private set; } = 0;
        public int EncryptedFileCount { get; private set; } = 0;
        
        public FAesKey? AesKey { get; set; }

        public bool IsEncrypted => Info.EncryptedIndex;

        public PakFileReader(FArchive Ar)
        {
            this.Ar = Ar;
            FileName = Ar.Name;
            Info = FPakInfo.ReadFPakInfo(Ar);
            if (Info.Version > EPakFileVersion.PakFile_Version_Latest)
            {
                log.Warning($"Pak file \"{FileName}\" has unsupported version {(int) Info.Version}");
            }
        }

        public PakFileReader(string filePath, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : this(new FileInfo(filePath), ver, game) {}
        public PakFileReader(FileInfo file, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : this(new FStreamArchive(file.Name, file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ver, game)) {}


        public byte[] Extract(FPakEntry file)
        {
            if (file.Pak != this) throw new ArgumentException($"Wrong pak file reader, required {file.Pak.FileName}, this is {FileName}");
            // If this reader is used as a concurrent reader create a clone of the main reader to provide thread safety
            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
            // Pak Entry is written before the file data,
            // but its the same as the one from the index, just without a name
            // We don't need to serialize that again so + file.StructSize
            reader.Position = file.Pos + file.StructSize;

            if (file.IsCompressed)
            {
#if DEBUG
                Log.Debug($"{file.Name} is compressed with {file.CompressionMethod}");
#endif
                var data = new MemoryStream((int) file.UncompressedSize);
                foreach (var block in file.CompressionBlocks)
                {
                    reader.Position = block.CompressedStart;
                    var srcSize = (int) (block.CompressedEnd - block.CompressedStart).Align(file.IsEncrypted ? Aes.ALIGN : 1);
                    // Read the compressed block
                    byte[] src = ReadAndDecrypt(srcSize, reader, file.IsEncrypted);
                    // Calculate the uncompressed size,
                    // its either just the compression block size
                    // or if its the last block its the remaining data size
                    var uncompressedSize = (int) Math.Min(file.CompressionBlockSize, (file.UncompressedSize - data.Length));
                    data.Write(Compression.Compression.Decompress(src, uncompressedSize, file.CompressionMethod, reader), 0, uncompressedSize);
                }

                if (data.Length == file.UncompressedSize) return data.GetBuffer();
                else if (data.Length > file.UncompressedSize) return data.GetBuffer().SubByteArray((int) file.UncompressedSize);
                else throw new ParserException(reader, $"Decompression of {file.Name} failed, {data.Length} < {file.UncompressedSize}");
            }
            else
            {
                // File might be encrypted or just stored normally
                var size = (int) file.UncompressedSize.Align(file.IsEncrypted ? Aes.ALIGN : 1);
                var data = ReadAndDecrypt(size, reader, file.IsEncrypted);
                return size != file.UncompressedSize ? data.SubByteArray((int) file.UncompressedSize) : data;
            }
        }

        public IReadOnlyDictionary<string, GameFile> ReadIndex(bool caseInsensitive = false)
        {
            var watch = new Stopwatch();
            watch.Start();
            if (Info.Version >= EPakFileVersion.PakFile_Version_PathHashIndex)
                ReadIndexUpdated(caseInsensitive);
            else
                ReadIndexLegacy(caseInsensitive);
            var elapsed = watch.Elapsed;
            var sb = new StringBuilder($"Pak {FileName}: {FileCount} files");
            if (EncryptedFileCount != 0)
                sb.Append($" ({EncryptedFileCount} encrypted)");
            if (MountPoint.Contains("/"))
                sb.Append($", mount point: \"{MountPoint}\"");
            sb.Append($", version {(int) Info.Version} in {elapsed}");
            log.Information(sb.ToString());
            return Files;
        }

        private IReadOnlyDictionary<string, GameFile> ReadIndexLegacy(bool caseInsensitive)
        {
            Ar.Position = Info.IndexOffset;
            var index = new FByteArchive($"{FileName} - Index", ReadAndDecrypt((int) Info.IndexSize));
            
            string mountPoint;
            try
            {
                mountPoint = index.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{FileName}'", e);
            }
            
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;
            FileCount = index.Read<int>();
            var files = new Dictionary<string, GameFile>(FileCount);

            for (int i = 0; i < FileCount; i++)
            {
                var path = index.ReadFString();
                var entry = new FPakEntry(this, path, index, Info);
                if (caseInsensitive)
                    files[path.ToLowerInvariant()] = entry;
                else
                    files[path] = entry;
            }

            Files = files;
            return files;
        }

        private IReadOnlyDictionary<string, GameFile> ReadIndexUpdated(bool caseInsensitive)
        {
            // Prepare primary index and decrypt if necessary
            Ar.Position = Info.IndexOffset;
            FArchive primaryIndex = new FByteArchive($"{FileName} - Primary Index", ReadAndDecrypt((int) Info.IndexSize));

            string mountPoint;
            try
            {
                mountPoint = primaryIndex.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{FileName}'", e);
            }
            
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            FileCount = primaryIndex.Read<int>();
            EncryptedFileCount = 0;

            primaryIndex.Position += 8; // PathHashSeed

            if (!primaryIndex.ReadBoolean())
                throw new ParserException(primaryIndex, "No path hash index");

            primaryIndex.Position += 36; // PathHashIndexOffset (long) + PathHashIndexSize (long) + PathHashIndexHash (20 bytes)

            if (!primaryIndex.ReadBoolean())
                throw new ParserException(primaryIndex, "No directory index");

            var directoryIndexOffset = primaryIndex.Read<long>();
            var directoryIndexSize = primaryIndex.Read<long>();
            primaryIndex.Position += 20; // Directory Index hash
            var encodedPakEntriesSize = primaryIndex.Read<int>();
            var encodedPakEntries = primaryIndex.ReadBytes(encodedPakEntriesSize);

            if (primaryIndex.Read<int>() < 0)
                throw new ParserException("Corrupt pak PrimaryIndex detected");

            // Read FDirectoryIndex
            Ar.Position = directoryIndexOffset;
            var directoryIndex = new FByteArchive($"{FileName} - Directory Index", ReadAndDecrypt((int) directoryIndexSize));

            unsafe { fixed(byte* ptr = encodedPakEntries) {
                var directoryIndexLength = directoryIndex.Read<int>();

                var files = new Dictionary<string, GameFile>(FileCount);
                
                for (int i = 0; i < directoryIndexLength; i++)
                {
                    var dir = directoryIndex.ReadFString();
                    var dirDictLength = directoryIndex.Read<int>();
                    
                    for (int j = 0; j < dirDictLength; j++)
                    {
                        var name = directoryIndex.ReadFString();
                        var path = string.Concat(mountPoint, dir, name);
                        var entry = new FPakEntry(this, path, ptr + directoryIndex.Read<int>());
                        if (entry.IsEncrypted)
                            EncryptedFileCount++;
                        if (caseInsensitive)
                            files[path.ToLowerInvariant()] = entry;
                        else
                            files[path] = entry;
                    }
                }

                Files = files;
                
                return files;
            } }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);
        private byte[] ReadAndDecrypt(int length, FArchive reader, bool isEncrypted)
        {
            if (isEncrypted)
            {
                if (AesKey != null)
                {
                    return reader.ReadBytes(length).Decrypt(AesKey);
                }
                throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
            }

            return reader.ReadBytes(length);
        }

        private void ValidateMountPoint(ref string mountPoint)
        {
            var badMountPoint = !mountPoint.StartsWith("../../..");
            mountPoint = mountPoint.SubstringAfter("../../..");
            if (mountPoint[0] != '/' || ( (mountPoint.Length > 1) && (mountPoint[1] == '.') ))
                badMountPoint = true;

            if (badMountPoint)
            {
                log.Warning($"Pak \"{FileName}\" has strange mount point \"{mountPoint}\", mounting to root");
                mountPoint = "/";
            }

            mountPoint = mountPoint.Substring(1);
        }

        public override string ToString() => FileName;
        
        private const int MAX_MOUNTPOINT_TEST_LENGTH = 128;

        public byte[] IndexCheckBytes()
        {
            Ar.Position = Info.IndexOffset;
            return Ar.ReadBytes((int) (4 + MAX_MOUNTPOINT_TEST_LENGTH * 2).Align(Aes.ALIGN));
        }
        
        
        public bool TestAesKey(FAesKey key) => !IsEncrypted ? true : TestAesKey(IndexCheckBytes(), key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex(byte[] testBytes) => IsValidIndex(new FByteArchive(string.Empty, testBytes));
        public static bool IsValidIndex(FArchive reader)
        {
            var mountPointLength = reader.Read<int>();
            if (mountPointLength > MAX_MOUNTPOINT_TEST_LENGTH || mountPointLength < -MAX_MOUNTPOINT_TEST_LENGTH)
                return false;
            // Calculate the pos of the null terminator for this string
            // Then read the null terminator byte and check whether it is actually 0
            if (mountPointLength == 0) return reader.Read<byte>() == 0;
            else if (mountPointLength < 0)
            {
                // UTF16
                reader.Seek(-(mountPointLength - 1) * 2, SeekOrigin.Current);
                return reader.Read<short>() == 0;
            }
            else
            {
                // UTF8
                reader.Seek(mountPointLength - 1, SeekOrigin.Current);
                return reader.Read<byte>() == 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestAesKey(byte[] bytes, FAesKey key) => IsValidIndex(bytes.Decrypt(key));
    }
}