using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Vfs;
using Serilog;

namespace CUE4Parse.UE4.IO
{
    public class IoStoreReader : AbstractAesVfsReader
    {
        private static readonly ILogger log = Log.ForContext<IoStoreReader>();

        public readonly FArchive Ar;
        
        public readonly FIoStoreTocResource TocResource;
        public readonly FIoStoreTocHeader Info;
        public string MountPoint { get; private set; }
        public override FGuid EncryptionKeyGuid => Info.EncryptionKeyGuid;
        public override bool IsEncrypted => Info.ContainerFlags.HasFlag(EIoContainerFlags.Encrypted);
        public bool HasDirectoryIndex => TocResource.DirectoryIndexBuffer != null;

        public IoStoreReader(FArchive containerStream, FArchive tocStream, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex) : 
            base(containerStream.Name, containerStream.Ver, containerStream.Game)
        {
            Ar = containerStream;
            TocResource = new FIoStoreTocResource(tocStream, readOptions);
            Info = TocResource.Header;
            if (TocResource.Header.Version > EIoStoreTocVersion.Latest)
            {
                log.Warning($"Io Store \"{Name}\" has unsupported version {(int) Info.Version}");
            }
        }
        
        public IoStoreReader(string containerPath, string tocPath, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : this(new FileInfo(containerPath), new FileInfo(tocPath), readOptions, ver, game) {}
        public IoStoreReader(FileInfo containerFile, FileInfo tocFile, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : this(new FStreamArchive(containerFile.Name, containerFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ver, game), new FByteArchive(tocFile.Name, File.ReadAllBytes(tocFile.FullName), ver, game), readOptions) {}

        public override byte[] Extract(VfsEntry entry)
        {
            throw new System.NotImplementedException();
        }

        public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
        {
            var watch = new Stopwatch();
            watch.Start();
            
            ProcessIndex(caseInsensitive);
            
            var elapsed = watch.Elapsed;
            var sb = new StringBuilder($"IoStore {Name}: {FileCount} files");
            if (EncryptedFileCount != 0)
                sb.Append($" ({EncryptedFileCount} encrypted)");
            if (MountPoint.Contains("/"))
                sb.Append($", mount point: \"{MountPoint}\"");
            sb.Append($", version {(int) Info.Version} in {elapsed}");
            log.Information(sb.ToString());
            return Files;
        }
        public IReadOnlyDictionary<string, GameFile> ProcessIndex(bool caseInsensitive)
        {
            if (!HasDirectoryIndex || TocResource.DirectoryIndexBuffer == null) throw new ParserException(Ar, "No directory index");
            var directoryIndex = new FByteArchive(Name, DecryptIfEncrypted(TocResource.DirectoryIndexBuffer));
            
            string mountPoint;
            try
            {
                mountPoint = directoryIndex.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{Name}'", e);
            }
            
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            var directoryEntries = directoryIndex.ReadArray<FIoDirectoryIndexEntry>();
            var fileEntries = directoryIndex.ReadArray<FIoFileIndexEntry>();
            var stringTable = directoryIndex.ReadArray(directoryIndex.ReadFString);

            ref var root = ref directoryEntries[0];

            var files = new Dictionary<string, GameFile>(fileEntries.Length);
            
            ReadIndex(string.Empty, root.FirstChildEntry);
            
            void ReadIndex(string directoryName, uint dir)
            {
                const uint invalidHandle = uint.MaxValue;

                while (dir != invalidHandle)
                {
                    ref var dirEntry = ref directoryEntries[dir];
                    var subDirectoryName = string.Concat(directoryName, stringTable[dirEntry.Name], '/');

                    var file = dirEntry.FirstFileEntry;
                    while (file != invalidHandle)
                    {
                        ref var fileEntry = ref fileEntries[file];
                        
                        var path = string.Concat(subDirectoryName, stringTable[fileEntry.Name]);
                        var entry = new FIoStoreEntry(this, path, fileEntry.UserData);
                        if (entry.IsEncrypted)
                            EncryptedFileCount++;
                        if (caseInsensitive)
                            files[path.ToLowerInvariant()] = entry;
                        else
                            files[path] = entry;

                        file = fileEntry.NextFileEntry;
                    }
                    
                    ReadIndex(subDirectoryName, dirEntry.FirstChildEntry);
                    dir = dirEntry.NextSiblingEntry;
                }
            }
            
            return Files = files;
        }

        public override byte[] MountPointCheckBytes() => TocResource.DirectoryIndexBuffer ?? new byte[MAX_MOUNTPOINT_TEST_LENGTH];
        protected override byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);

        public override void Dispose()
        {
            Ar.Dispose();
        }
    }
}