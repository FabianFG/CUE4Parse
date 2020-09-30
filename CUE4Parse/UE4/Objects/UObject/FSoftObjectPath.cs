using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.UObject
{
    public readonly struct FSoftObjectPath : IUStruct
    {
        /** Asset path, patch to a top level object in a package. This is /package/path.assetname */
        public readonly FName AssetPathName;
        /** Optional FString for subobject within an asset. This is the sub path after the : */
        public readonly string SubPathString;

        public FSoftObjectPath(FAssetArchive Ar)
        {
            if (Ar.Ver < UE4Version.VER_UE4_ADDED_SOFT_OBJECT_PATH)
            {
                var path = Ar.ReadFString();
                throw new ParserException($"Asset path \"{path}\" is in short form and is not supported, nor recommended");
            }
            else
            {
                AssetPathName = Ar.ReadFName();
                SubPathString = Ar.ReadFString();
            }
        }
    }
}
