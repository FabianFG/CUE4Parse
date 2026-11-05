using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.UObject
{
    public readonly struct FStateFrame : IUStruct
    {
        public FStateFrame(FArchive Ar)
        {
            var node = 0;
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.Release51)
            {
                node = Ar.Read<int>(); // FPackageIndex - Node
                Ar.Position += sizeof(int); // FPackageIndex - StateNode
            }
            else
            {
                var oldClass = Ar.Read<int>(); // FPackageIndex - OldClass
                if (oldClass != 0)
                {
                    Ar.Position += sizeof(int); // int - iOldNode
                }
            }

            if (Ar.Ver < EUnrealEngineObjectUE3Version.Release52)
            {
                Ar.Position += sizeof(int); // FPackageIndex - Tmp
            }

            if (Ar.Ver < EUnrealEngineObjectUE3Version.REDUCED_PROBEMASK_REMOVED_IGNOREMASK)
            {
                Ar.Position += sizeof(long); // long - ProbeMask
            }
            else
            {
                Ar.Position += sizeof(int); // int - ProbeMask
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.REDUCED_STATEFRAME_LATENTACTION_SIZE)
            {
                Ar.Position += sizeof(short); // short - LatentAction
            }
            else if (Ar.Ver >= EUnrealEngineObjectUE3Version.Release55)
            {
                Ar.Position += sizeof(int); // int - LatentAction
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.AddedStateStackToUStateFrame)
            {
                Ar.SkipFixedArray(9); // StateStack
            }

            if (node != 0)
            {
                Ar.Position += sizeof(int);
            }
        }
    }
}
