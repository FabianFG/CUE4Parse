using CUE4Parse.UE4.Objects.Core.Misc;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.LevelSequence
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FLevelSequenceLegacyObjectReference : IUStruct
    {
        /** Primary method of resolution - object ID, stored as an annotation on the object in the world, resolvable through TLazyObjectPtr */
        public readonly FGuid ObjectId; // FUniqueObjectGuid but it's just a wrapper around FGuid
        /** Secondary method of resolution - path to the object within the context */
        public readonly string ObjectPath;

        public FLevelSequenceLegacyObjectReference(FArchive Ar)
        {
            ObjectId = Ar.Read<FGuid>();
            ObjectPath = Ar.ReadFString();
        }
    }
}