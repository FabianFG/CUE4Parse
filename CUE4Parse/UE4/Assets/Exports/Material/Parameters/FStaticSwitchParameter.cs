using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    [StructFallback]
    public class FStaticSwitchParameter : FStaticParameterBase
    {
        public bool Value;

        public FStaticSwitchParameter(FArchive Ar) : base(Ar)
        {
            Value = Ar.ReadBoolean();
            bOverride = Ar.ReadBoolean();
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.MATERIAL_FALLBACKS)
            {
                ExpressionGuid = Ar.Read<FGuid>();
            }
        }

        public FStaticSwitchParameter(FStructFallback fallback) : base(fallback)
        {
            Value = fallback.GetOrDefault<bool>(nameof(Value));
        }
    }
}
