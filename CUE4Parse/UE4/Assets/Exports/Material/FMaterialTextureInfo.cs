using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Assets.Exports.Material;

[StructFallback]
public class FMaterialTextureInfo : IUStruct, ISerializable
{
    public readonly float SamplingScale;
    public readonly int UVChannelIndex;
    public readonly FName TextureName;

    public FMaterialTextureInfo(FStructFallback fallback)
    {
        SamplingScale = fallback.GetOrDefault<float>(nameof(SamplingScale));
        UVChannelIndex = fallback.GetOrDefault<int>(nameof(UVChannelIndex));
        TextureName = fallback.GetOrDefault<FName>(nameof(TextureName));
    }

    public FMaterialTextureInfo(FAssetArchive Ar)
    {
        SamplingScale = Ar.Read<float>();
        UVChannelIndex = Ar.Read<int>();
        TextureName = Ar.ReadFName();
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(SamplingScale);
        Ar.Write(UVChannelIndex);
        Ar.Serialize(TextureName);
    }

    public override string ToString() => $"{UVChannelIndex}: {TextureName} (x{SamplingScale})";
}