using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Flash;

public class USwfMovie : UObject
{
    public bool bUseGFxExport;
    public bool bPackTextures;
    public int PackTextureSize;
    public EFlashTextureRescale TextureRescale;
    public string TextureFormat = string.Empty;
    public string SourceFile = string.Empty;
    public string SourceFileTimestamp = string.Empty;
    public byte[] RawData = []; // Shockwave Flash (.swf)
    public string[] ReferencedAssetNames = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bUseGFxExport = GetOrDefault<bool>(nameof(bUseGFxExport));
        bPackTextures = GetOrDefault<bool>(nameof(bPackTextures));
        PackTextureSize = GetOrDefault<int>(nameof(PackTextureSize));
        TextureRescale = GetOrDefault<EFlashTextureRescale>(nameof(TextureRescale));
        TextureFormat = GetOrDefault<string>(nameof(TextureFormat));
        SourceFile = GetOrDefault<string>(nameof(SourceFile));
        SourceFileTimestamp = GetOrDefault<string>(nameof(SourceFileTimestamp));
        RawData = GetOrDefault<byte[]>(nameof(RawData), []);
        ReferencedAssetNames = GetOrDefault<string[]>(nameof(ReferencedAssetNames));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
    }
}

public enum EFlashTextureRescale : byte
{
    FlashTextureScale_High,
    FlashTextureScale_Low,
    FlashTextureScale_NextLow,
    FlashTextureScale_Mult4,
    FlashTextureScale_None
}
