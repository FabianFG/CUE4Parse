using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Animation
{
    public readonly struct FSmartName : IUStruct
    {
        public readonly FName DisplayName;

        public FSmartName(FAssetArchive Ar)
        {
			DisplayName = Ar.ReadFName();
		}
    }
}
