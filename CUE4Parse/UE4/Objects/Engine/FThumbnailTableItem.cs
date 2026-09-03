using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine
{
    public readonly struct FThumbnailTableItem : IUStruct
    {
        public readonly string ObjectClassName;
        public readonly string ObjectPath;
        public readonly int ThumbnailOffset;

        public FThumbnailTableItem(FArchive Ar)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.CONTENT_BROWSER_FULL_NAMES)
            {
                ObjectClassName = Ar.ReadFString();
            }
            ObjectPath = Ar.ReadFString();
            ThumbnailOffset = Ar.Read<int>();
        }
    }
}
