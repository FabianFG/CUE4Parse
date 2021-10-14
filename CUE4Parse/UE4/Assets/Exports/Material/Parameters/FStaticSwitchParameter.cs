using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    public class FStaticSwitchParameter : FStaticParameterBase
    {
        public bool Value;

        public FStaticSwitchParameter(FArchive Ar) : base(Ar)
        {
            Value = Ar.ReadBoolean();
            bOverride = Ar.ReadBoolean();
            ExpressionGuid = Ar.Read<FGuid>();
        }
    }
}
