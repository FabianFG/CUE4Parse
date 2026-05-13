using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using FIoBlockHash = uint;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FOnDemandTocContainerEntry
    {
        public readonly FOnDemandTocEntry[] Entries;
        public readonly uint[] BlockSizes;
        public readonly FIoBlockHash[] BlockHashes;
        public readonly byte[] Header;
        public readonly EOnDemandContainerFlags ContainerFlags;

        public readonly FGuid EncryptionKeyGuid;
        public readonly FIoContainerId ContainerId;
        public readonly string ContainerName;
        public readonly string LegacyEncryptionKeyGuid;
        public readonly FSHAHash UTocHash;

        public readonly uint ContainerHeaderSize;
        public readonly uint DataOffset;
        public readonly uint DataSize;
        public readonly uint ChunkCount;
        public readonly uint BlockCount;
        public readonly uint BlockSize;
        public readonly uint TagSetCount;
        public readonly uint TagSetIndicesCount;
        public readonly EOnDemandContainerEntryFlags ContainerEntryFlags;
        public readonly EIoContainerFlags FileContainerFlags;

        public FOnDemandContainerData ContainerData;
        
        public FOnDemandTocContainerEntry(FArchive Ar, FOnDemandTocHeader header)
        {
            if (header.IsLegacy)
            {
                if (header.LegacyVersion >= EOnDemandTocVersion.ContainerId)
                {
                    ContainerId = Ar.Read<FIoContainerId>();
                }

                ContainerName = Ar.ReadFString();
                LegacyEncryptionKeyGuid = Ar.ReadFString();
                Entries = Ar.ReadArray(() => new FOnDemandTocEntry(Ar));
                BlockSizes = Ar.ReadArray<uint>();
                BlockHashes = Ar.ReadArray<FIoBlockHash>();
                UTocHash = new FSHAHash(Ar);

                if (header.LegacyVersion >= EOnDemandTocVersion.ContainerFlags)
                {
                    ContainerFlags = Ar.Read<EOnDemandContainerFlags>();
                }

                if (header.LegacyVersion >= EOnDemandTocVersion.ContainerHeader)
                {
                    Header = Ar.ReadArray<byte>();
                }
            }
            else
            {

                EncryptionKeyGuid = Ar.Read<FGuid>();

                ContainerId = Ar.Read<FIoContainerId>();

                var NameEntry = new FOnDemandStringEntry(Ar);

                ContainerHeaderSize = Ar.Read<uint>();
                DataOffset = Ar.Read<uint>();
                DataSize = Ar.Read<uint>();
                ChunkCount = Ar.Read<uint>();
                BlockCount = Ar.Read<uint>();
                BlockSize = Ar.Read<uint>();
                TagSetCount = Ar.Read<uint>();
                TagSetIndicesCount = Ar.Read<uint>();
                UTocHash = new FSHAHash(Ar);
                ContainerEntryFlags = Ar.Read<EOnDemandContainerEntryFlags>();
                FileContainerFlags = Ar.Read<EIoContainerFlags>();

                Ar.Position += 36;

                ContainerName = header.GetString(NameEntry);
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
        WithSoftReferences		= (1 << 5),
        PendingHostGroup		= (1 << 6),
        Last = PendingHostGroup
    }

    [Flags]
    public enum EOnDemandContainerEntryFlags : uint
    {
        None = 0,
        InstallOnDemand = (1 << 0),
        StreamOnDemand = (1 << 1),
    }
}
