using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.GTA
{
    public class GScalarParameterValue : IUStruct
    {
        public FMaterialParameterInfo ParameterInfo;
        public float ParameterValue;
        public FGuid ExpressionGUID;

        public GScalarParameterValue(FAssetArchive Ar)
        {
            ParameterInfo = new FMaterialParameterInfo(Ar);
            ParameterValue = Ar.Read<float>();
            ExpressionGUID = Ar.Read<FGuid>();
        }
    }
}
