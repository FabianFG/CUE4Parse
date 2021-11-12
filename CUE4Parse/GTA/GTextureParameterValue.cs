using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GTA
{
    public struct GTextureParameterValue : IUStruct
    {
        public FName ParameterInfo;
        public int Value1;
        public int Value2;
        public int Value3;
        public FGuid ExpressionGUID;

        public GTextureParameterValue(FAssetArchive Ar)
        {
            ParameterInfo = Ar.ReadFName();

            Value1 = Ar.ReadByte();
            Value2 = Ar.Read<int>();
            Value3 = Ar.Read<int>();

            ExpressionGUID = Ar.Read<FGuid>();
        }
    }
}
