using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters
{
    [StructFallback]
    public class FStaticParameterBase
    {
        [JsonIgnore]
        public string Name => ParameterInfo?.Name.Text ?? "None";
        public FMaterialParameterInfo? ParameterInfo;
        public bool bOverride;
        public FGuid ExpressionGuid;

        public FStaticParameterBase() { }

        public FStaticParameterBase(FStructFallback fallback)
        {
            ParameterInfo = fallback.GetOrDefault<FMaterialParameterInfo>(nameof(ParameterInfo));
            bOverride = fallback.GetOrDefault<bool>(nameof(bOverride));
            ExpressionGuid = fallback.GetOrDefault<FGuid>(nameof(ExpressionGuid));
        }

        public FStaticParameterBase(FArchive Ar)
        {
            ParameterInfo = FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MaterialAttributeLayerParameters ? new FMaterialParameterInfo { Name = Ar.ReadFName() } : new FMaterialParameterInfo(Ar);
        }
    }
}