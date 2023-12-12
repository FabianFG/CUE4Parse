using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

[StructFallback, JsonConverter(typeof(FMaterialParameterInfoConverter))]
public class FMaterialParameterInfo : ISerializable
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

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(Name);
        Ar.Write((byte) Association);
        Ar.Write(Index);
    }
}

public enum EMaterialParameterAssociation : byte
{
    LayerParameter,
    BlendParameter,
    GlobalParameter
}