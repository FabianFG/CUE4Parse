using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    public class FStaticParameterBase
    {
        public FMaterialParameterInfo? ParameterInfo;
        public bool bOverride;
        public FGuid ExpressionGuid;

        public FStaticParameterBase()
        {
        }

        public FStaticParameterBase(FArchive Ar)
        {
            ParameterInfo = FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MaterialAttributeLayerParameters ? new FMaterialParameterInfo { Name = Ar.ReadFName() } : new FMaterialParameterInfo(Ar);
        }
    }
}
