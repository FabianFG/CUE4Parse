using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback, JsonConverter(typeof(FMaterialParameterInfoConverter))]
    public class FMaterialParameterInfo
    {
        public FName Name;
        public EMaterialParameterAssociation Association;
        public int Index;

        public FMaterialParameterInfo(FStructFallback fallback)
        {
            Name = fallback.GetOrDefault<FName>(nameof(Name));
            Association = fallback.GetOrDefault<EMaterialParameterAssociation>(nameof(Association));
            Index = fallback.GetOrDefault<int>(nameof(Index));
        }

        public FMaterialParameterInfo(FArchive Ar)
        {
            Name = Ar.ReadFName();
            Association = Ar.Read<EMaterialParameterAssociation>();
            Index = Ar.Read<int>();
        }

        public FMaterialParameterInfo()
        {
            Name = new FName();
            Association = EMaterialParameterAssociation.LayerParameter;
            Index = 0;
        }
    }

    public enum EMaterialParameterAssociation : byte
    {
        LayerParameter,
        BlendParameter,
        GlobalParameter
    }
}
