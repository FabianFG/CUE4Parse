using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FOnDemandTocContainerEntry
    {
        public readonly string ContainerName;
        public readonly string EncryptionKeyGuid;
        public readonly FOnDemandTocEntry[] Entries;
        public readonly uint[] BlockSizes;
        public readonly FSHAHash[] BlockHashes;
        public readonly FSHAHash UTocHash;

        public FOnDemandTocContainerEntry(FArchive Ar)
        {
            ContainerName = Ar.ReadFString();
            EncryptionKeyGuid = Ar.ReadFString();
            Entries = Ar.ReadArray(() => new FOnDemandTocEntry(Ar));
            BlockSizes = Ar.ReadArray<uint>();
            BlockHashes = Ar.ReadArray<FSHAHash>();
            UTocHash = Ar.Read<FSHAHash>();
        }
    }
}
