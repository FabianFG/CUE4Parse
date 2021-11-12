using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.GTA
{
    public class GTextureParameterValue : FTextureParameterValue, IUStruct
    {
        public FGuid ExpressionGUID;

        public GTextureParameterValue(FAssetArchive Ar) : base(Ar)
        {
            ExpressionGUID = Ar.Read<FGuid>();
        }
    }
}
