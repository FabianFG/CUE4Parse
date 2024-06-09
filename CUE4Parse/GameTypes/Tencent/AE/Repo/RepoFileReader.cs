using System;
using System.Collections.Generic;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.Tencent.AE.Repo;

public class RepoFileReader(FArchive Ar) : AbstractAesVfsReader(Ar.Name, Ar.Versions)
{
    private FArchive Ar = Ar;
    public RepoIndex Index = new(Ar);

    public sealed override long Length { get; set; } = Ar.Length;
    public override bool HasDirectoryIndex => true;
    public override string MountPoint { get; protected set; } = string.Empty; // No mount point needed, just root
    public override FGuid EncryptionKeyGuid => default;
    public override bool IsEncrypted => false;

    public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
    {
        return Files;
    }

    public override byte[] Extract(VfsEntry entry)
    {
        throw new System.NotImplementedException();
    }

    public override void Dispose()
    {
        throw new System.NotImplementedException();
    }

    public override byte[] MountPointCheckBytes() => throw new NotImplementedException();

    protected override byte[] ReadAndDecrypt(int length)
    {
        throw new System.NotImplementedException();
    }
}