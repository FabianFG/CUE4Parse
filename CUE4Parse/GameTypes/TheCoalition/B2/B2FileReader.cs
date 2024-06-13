using System.Collections.Generic;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.TheCoalition.B2;

public class B2FileReader : AbstractAesVfsReader
{
    public B2Index Index;

    public B2FileReader(string path, VersionContainer versions) : base(path, versions)
    {
    }

    public override bool HasDirectoryIndex { get; }
    public override string MountPoint { get; protected set; }
    public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
    {
        throw new System.NotImplementedException();
    }

    public override byte[] Extract(VfsEntry entry)
    {
        throw new System.NotImplementedException();
    }

    public override void Dispose()
    {
        throw new System.NotImplementedException();
    }

    public override FGuid EncryptionKeyGuid { get; }
    public override bool IsEncrypted { get; }
    public override byte[] MountPointCheckBytes()
    {
        throw new System.NotImplementedException();
    }

    protected override byte[] ReadAndDecrypt(int length)
    {
        throw new System.NotImplementedException();
    }

    public override long Length { get; set; }
}