using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback]
    public class FMaterialParameterInfo
    {
        public readonly FName Name;
        public readonly EMaterialParameterAssociation Association;
        public readonly int Index;

        public FMaterialParameterInfo(FStructFallback fallback)
        {
            Name = fallback.GetOrDefault<FName>(nameof(Name));
            Association = fallback.GetOrDefault<EMaterialParameterAssociation>(nameof(Association));
            Index = fallback.GetOrDefault<int>(nameof(Index));
        }
    }

    public enum EMaterialParameterAssociation : byte
    {
        LayerParameter,
        BlendParameter,
        GlobalParameter
    }
}