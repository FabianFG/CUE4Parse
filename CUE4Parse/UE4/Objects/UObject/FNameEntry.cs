using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    public readonly struct FNameEntry
    {
        public readonly string Name;
#if NAME_HASHES
        public readonly ushort NonCasePreservingHash;
        public readonly ushort CasePreservingHash;        
#endif
        public FNameEntry(FArchive Ar)
        {
            Name = Ar.ReadFString();
#if NAME_HASHES
            NonCasePreservingHash = Ar.Read<ushort>();
            CasePreservingHash = Ar.Read<ushort>();            
#else
            Ar.Position += 4;
#endif
        }

        public override string ToString()
        {
            return Name;
        }
    }
}