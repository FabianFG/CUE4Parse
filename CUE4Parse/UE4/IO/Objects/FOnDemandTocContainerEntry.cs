using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FOnDemandTocContainerEntry
    {
        public FIoContainerId ContainerId;
        public readonly string ContainerName;
        public readonly string EncryptionKeyGuid;
        public readonly FOnDemandTocEntry[] Entries;
        public readonly uint[] BlockSizes;
        public readonly uint[] BlockHashes; // FIoBlockHash is just uint32
        public readonly FSHAHash UTocHash;
        public readonly EOnDemandContainerFlags ContainerFlags;

        public FOnDemandTocContainerEntry(FArchive Ar, EOnDemandTocVersion version)
        {
            if (version >= EOnDemandTocVersion.ContainerId)
            {
                ContainerId = Ar.Read<FIoContainerId>();
            }
            
            ContainerName = Ar.ReadFString();
            EncryptionKeyGuid = Ar.ReadFString();
            Entries = Ar.ReadArray(() => new FOnDemandTocEntry(Ar));
            BlockSizes = Ar.ReadArray<uint>();
            BlockHashes = Ar.ReadArray<uint>();
            UTocHash = new FSHAHash(Ar);
            
            if (version >= EOnDemandTocVersion.ContainerFlags)
            {
                ContainerFlags = Ar.Read<EOnDemandContainerFlags>();
            }
        }
    }
    
    [Flags]
    public enum EOnDemandContainerFlags : byte
    {
        None					= 0,
        PendingEncryptionKey	= (1 << 0),
        Mounted					= (1 << 1),
        StreamOnDemand			= (1 << 2),
        InstallOnDemand			= (1 << 3),
        Encrypted				= (1 << 4),
        Count
    }
}