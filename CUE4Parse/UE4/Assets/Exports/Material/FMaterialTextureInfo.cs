using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public readonly struct FMaterialTextureInfo : IUStruct
    {
        public readonly float SamplingScale;
        public readonly int UVChannelIndex;
        public readonly FName TextureName;

        public FMaterialTextureInfo(FAssetArchive Ar)
        {
            SamplingScale = Ar.Read<float>();
            UVChannelIndex = Ar.Read<int>();
            TextureName = Ar.ReadFName();
        }
    }
}
