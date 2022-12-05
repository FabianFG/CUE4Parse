using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public readonly struct FKeyType : IUStruct
    {
        public readonly FName AttributeName;
        public readonly FName GroupName;

        public FKeyType(FAssetArchive Ar)
        {
            AttributeName = Ar.ReadFName();
            GroupName = Ar.ReadFName();
        }
    }
}
