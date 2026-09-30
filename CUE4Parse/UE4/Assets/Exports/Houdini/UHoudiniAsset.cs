using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Houdini;

public class UHoudiniAsset : UObject
{
    public uint FileFormatVersion;
    public uint AssetFlags;
    public string AssetFileName = string.Empty;

    public byte[] HdaBuffer = []; // Houdini Digital Asset (.hda)

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        FileFormatVersion = Ar.Read<uint>();
        HdaBuffer = Ar.ReadArray<byte>();
        AssetFlags = Ar.Read<uint>();
        AssetFileName = Ar.ReadFString();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(FileFormatVersion));
        writer.WriteValue(FileFormatVersion);

        writer.WritePropertyName(nameof(AssetFlags));
        writer.WriteValue(AssetFlags);

        writer.WritePropertyName(nameof(AssetFileName));
        writer.WriteValue(AssetFileName);
    }
}
