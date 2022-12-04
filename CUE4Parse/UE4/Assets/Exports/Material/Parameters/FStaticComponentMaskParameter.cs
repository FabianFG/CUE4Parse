using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    public class FStaticComponentMaskParameter : FStaticParameterBase
    {
        public bool R, G, B, A;

        public FStaticComponentMaskParameter() : base()
        {

        }

        public FStaticComponentMaskParameter(FArchive Ar) : base(Ar)
        {
            R = Ar.ReadBoolean();
            G = Ar.ReadBoolean();
            B = Ar.ReadBoolean();
            A = Ar.ReadBoolean();
            bOverride = Ar.ReadBoolean();
            ExpressionGuid = Ar.Read<FGuid>();
        }
    }
}
