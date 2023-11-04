using System.Collections.Generic;
using System.IO;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.UE4.IO
{
    public class IoStoreOnDemandReader : AbstractAesVfsReader
    {
        public readonly FOnDemandTocHeader Header;
        public readonly FTocMeta Meta;
        public readonly FOnDemandTocContainerEntry[] Containers;

        public override string MountPoint { get; protected set; }
        public sealed override long Length { get; set; }

        public override bool HasDirectoryIndex { get; }
        public override FGuid EncryptionKeyGuid { get; }
        public override bool IsEncrypted { get; }

        public IoStoreOnDemandReader(string iochunktocPath, VersionContainer? versions = null)
            : this(new FileInfo(iochunktocPath), versions) { }
        public IoStoreOnDemandReader(FileInfo iochunktocFile, VersionContainer? versions = null)
            : this(new FByteArchive(iochunktocFile.FullName, File.ReadAllBytes(iochunktocFile.FullName), versions)) { }

        public IoStoreOnDemandReader(FArchive iochunktocStream) : base(iochunktocStream.Name, iochunktocStream.Versions)
        {
            Length = iochunktocStream.Length;
            Header = new FOnDemandTocHeader(iochunktocStream);

            if (Header.Version >= EOnDemandTocVersion.Meta)
                Meta = new FTocMeta(iochunktocStream);

            Containers = iochunktocStream.ReadArray(() => new FOnDemandTocContainerEntry(iochunktocStream));
        }

        public override byte[] Extract(VfsEntry entry)
        {
            throw new System.NotImplementedException();
        }

        public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
        {
            throw new System.NotImplementedException();
        }

        public override byte[] MountPointCheckBytes()
        {
            throw new System.NotImplementedException();
        }

        protected override byte[] ReadAndDecrypt(int length)
        {
            throw new System.NotImplementedException();
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
