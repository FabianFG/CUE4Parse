using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    public class FStaticMaterialLayersParameter : FStaticParameterBase
    {
        public FMaterialLayersFunctions? Value;

        public FStaticMaterialLayersParameter()
        {

        }

        public FStaticMaterialLayersParameter(FArchive Ar)
        {
            ParameterInfo = new FMaterialParameterInfo(Ar);
            bOverride = Ar.ReadBoolean();
            ExpressionGuid = Ar.Read<FGuid>();

            if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MaterialLayersParameterSerializationRefactor)
            {
                Value = new FMaterialLayersFunctions(Ar);
            }
        }
    }

    public class FMaterialLayersFunctions
    {
        public string KeyString; // Deprecated

        public FMaterialLayersFunctions(FArchive Ar)
        {
            KeyString = Ar.ReadFString();
        }
    }
}
