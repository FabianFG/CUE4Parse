using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class UMaterialExpressionParameter : UMaterialExpression
    {
        public FName ParameterName { get; private set; }
        public FGuid ExpressionGUID { get; private set; }
        public FName Group { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            ParameterName = GetOrDefault<FName>(nameof(ParameterName));
            ExpressionGUID = GetOrDefault<FGuid>(nameof(ExpressionGUID));
            Group = GetOrDefault<FName>(nameof(Group));
        }
    }

    public class UMaterialExpressionVectorParameter : UMaterialExpressionParameter
    {
        public FLinearColor DefaultValue { get; private set; }
        public FParameterChannelNames ChannelNames { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            DefaultValue = GetOrDefault<FLinearColor>(nameof(DefaultValue));
            ChannelNames = GetOrDefault<FParameterChannelNames>(nameof(ChannelNames));
        }
    }

    public class UMaterialExpressionScalarParameter : UMaterialExpressionParameter
    {
        public float DefaultValue { get; private set; }
        public float SliderMin { get; private set; }
        public float SliderMax { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            DefaultValue = GetOrDefault<float>(nameof(DefaultValue));
            SliderMin = GetOrDefault<float>(nameof(SliderMin));
            SliderMax = GetOrDefault<float>(nameof(SliderMax));
        }
    }

    public class UMaterialExpressionStaticBoolParameter : UMaterialExpressionParameter
    {
        public bool DefaultValue { get; private set; } = true;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            DefaultValue = GetOrDefault<bool>(nameof(DefaultValue));
        }
    }
}
