using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GTA
{
    public struct GMaterialTextureInfo : IUStruct
    {
        public float Value1;
        public float Value2;
        public FName DiffuseTexture;

        public GMaterialTextureInfo(FAssetArchive Ar)
        {
            Value1 = Ar.Read<float>(); // 1
            Value2 = Ar.Read<float>(); // 0
            DiffuseTexture = Ar.ReadFName(); // Diffuse Texture Name
        }
    }
}
