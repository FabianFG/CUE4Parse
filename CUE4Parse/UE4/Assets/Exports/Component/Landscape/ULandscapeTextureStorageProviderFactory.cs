using System;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeTextureStorageProviderFactory : UTextureAllMipDataProviderFactory
{
    public int NumNonOptionalMips { get; private set; }
    public int NumNonStreamingMips { get; private set; }
    public FVector LandscapeGridScale { get; private set; }
    public FLandscapeTexture2DMipMap[] Mips { get; private set; }
    public FPackageIndex Texture { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        NumNonOptionalMips = Ar.Read<int>();
        NumNonStreamingMips = Ar.Read<int>();
        LandscapeGridScale = new FVector(Ar);

        Mips = Ar.ReadArray(() => new FLandscapeTexture2DMipMap(Ar));

        Texture = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("NumNonOptionalMips");
        writer.WriteValue(NumNonOptionalMips);

        writer.WritePropertyName("NumNonStreamingMips");
        writer.WriteValue(NumNonStreamingMips);

        writer.WritePropertyName("LandscapeGridScale");
        serializer.Serialize(writer, LandscapeGridScale);

        writer.WritePropertyName("Mips");
        serializer.Serialize(writer, Mips);

        writer.WritePropertyName("Texture");
        serializer.Serialize(writer, Texture);
    }

    public void DecompressMip(byte[] SourceData, long SourceDataBytes, byte[] DestData, long DestDataBytes, int MipIndex)
    {
        var Mip = Mips[MipIndex];
        if (!Mip.bCompressed)
        {
            // mip is uncompressed, just copy it
            Array.Copy(SourceData, 0, DestData, 0, DestDataBytes);
            return;
        }

        var TotalPixels = Mip.SizeX * Mip.SizeY;
    }
}
